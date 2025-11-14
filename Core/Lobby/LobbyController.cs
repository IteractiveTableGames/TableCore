using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using TableCore.Core;
using TableCore.Core.Modules;
using TableCore.Input;

namespace TableCore.Lobby
{
	/// <summary>
	/// Controls the lobby scene where players can claim seats by touching near an edge of the table.
	/// </summary>
	public partial class LobbyController : Control
	{
		private const long MouseTouchKey = -1;
		private const string RuntimeScenePath = "res://Core/Runtime/ModuleRuntimeRoot.tscn";

		[Export(PropertyHint.Range, "0.2,3.0,0.1")]
		public float HoldDurationSeconds { get; set; } = 1.0f;

		[Export(PropertyHint.Range, "20,400,5")]
		public float EdgeJoinMargin { get; set; } = 120.0f;

		[Export(PropertyHint.Range, "120,540,10")]
		public float HudStripThickness { get; set; } = 320.0f;

		[Export(PropertyHint.Range, "200,1200,10")]
		public float HudStripLength { get; set; } = 520.0f;

		[Export(PropertyHint.Range, "2,60,1")]
		public float SeatIndicatorThickness { get; set; } = 12.0f;

		[Export(PropertyHint.Range, "0.05,0.5,0.05")]
		public float MaxSeatShiftFraction { get; set; } = 0.35f;

		[Export]
		public string JoinPromptText { get; set; } = "Touch and hold near an edge to join the table";

		[Export(PropertyHint.Dir)]
		public string ModulesDirectory { get; set; } = "res://Modules";

		private readonly Dictionary<long, TouchTracker> _activeTouches = new();
		private readonly SessionState _sessionState = new();
		private readonly Dictionary<Guid, SeatIndicatorElements> _seatIndicators = new();
		private readonly Dictionary<Guid, PlayerCustomizationHud> _customizationHuds = new();
		private readonly HashSet<Guid> _completedCustomizations = new();
		private readonly Color[] _playerPalette =
		{
			Color.FromHtml("F94144"),
			Color.FromHtml("F3722C"),
			Color.FromHtml("F9C74F"),
			Color.FromHtml("90BE6D"),
			Color.FromHtml("577590")
		};
		private readonly AvatarOption[] _avatarOptions = CreateDefaultAvatarOptions();

		private RichTextLabel? _playerListLabel;
		private Label? _promptLabel;
		private Label? _statusLabel;
		private InputRouter? _inputRouter;
		private Control? _seatOverlayRoot;
		private Control? _playerHudRoot;
		private PackedScene? _customizationHudScene;
		private PackedScene? _seatIndicatorScene;
		private ModuleSelectionModel _moduleSelectionModel = default!;
		private ItemList? _moduleList;
		private TextureRect? _moduleIcon;
		private Label? _moduleNameLabel;
		private Label? _modulePlayersLabel;
		private Label? _moduleSummaryLabel;
		private Control? _moduleEmptyState;
		private Button? _startGameButton;
		private readonly ModuleLoader _moduleLoader = new();

		public SessionState Session => _sessionState;

		public override void _Ready()
		{
			SetProcess(true);
			SetProcessInput(true);

			_playerListLabel = GetNodeOrNull<RichTextLabel>("PlayerPanel/PlayerList");
			_promptLabel = GetNodeOrNull<Label>("Prompts/JoinPrompt");
			_statusLabel = GetNodeOrNull<Label>("Prompts/StatusMessage");
			_inputRouter = GetNodeOrNull<InputRouter>("InputRouter");
			_seatOverlayRoot = GetNodeOrNull<Control>("SeatOverlays");
			_playerHudRoot = GetNodeOrNull<Control>("PlayerHUDRoot");
			_customizationHudScene ??= ResourceLoader.Load<PackedScene>("res://Core/Lobby/PlayerCustomizationHud.tscn");
			_seatIndicatorScene ??= ResourceLoader.Load<PackedScene>("res://Core/Lobby/SeatIndicator.tscn");
			_moduleList = GetNodeOrNull<ItemList>("ModulePanel/Content/ModuleListContainer/ModuleList");
			_moduleIcon = GetNodeOrNull<TextureRect>("ModulePanel/Content/ModuleDetails/Icon");
			_moduleNameLabel = GetNodeOrNull<Label>("ModulePanel/Content/ModuleDetails/Name");
			_modulePlayersLabel = GetNodeOrNull<Label>("ModulePanel/Content/ModuleDetails/Players");
			_moduleSummaryLabel = GetNodeOrNull<Label>("ModulePanel/Content/ModuleDetails/Summary");
			_moduleEmptyState = GetNodeOrNull<Control>("ModulePanel/Content/EmptyState");
			_startGameButton = GetNodeOrNull<Button>("ModulePanel/Content/StartButton");
			_moduleSelectionModel = new ModuleSelectionModel(_sessionState);

			if (_promptLabel != null)
			{
				_promptLabel.Text = JoinPromptText;
			}

			if (_moduleList != null)
			{
				_moduleList.ItemSelected += OnModuleListItemSelected;
			}

			if (_startGameButton != null)
			{
				_startGameButton.Pressed += OnStartGamePressed;
			}

			RefreshPlayerDisplay();
			UpdateInputRouterSession();
			LoadModuleCatalog();
			RefreshStartButtonState();
		}

		public override void _Input(InputEvent @event)
		{
			base._Input(@event);

			switch (@event)
			{
				case InputEventScreenTouch screenTouch:
					if (IsUiInteraction(screenTouch.Position))
					{
						if (!screenTouch.Pressed)
						{
							_activeTouches.Remove(screenTouch.Index);
						}
						return;
					}
					HandleScreenTouch(screenTouch);
					break;
				case InputEventScreenDrag screenDrag:
					if (IsUiInteraction(screenDrag.Position))
					{
						return;
					}
					HandleScreenDrag(screenDrag);
					break;
				case InputEventMouseButton mouseButton:
					if (IsUiInteraction(mouseButton.Position))
					{
						if (!mouseButton.Pressed)
						{
							_activeTouches.Remove(MouseTouchKey);
						}
						return;
					}
					HandleMouseButton(mouseButton);
					break;
				case InputEventMouseMotion mouseMotion:
					UpdateMousePosition(mouseMotion);
					break;
			}
		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			if (_activeTouches.Count == 0)
			{
				return;
			}

			var completedTouches = new List<long>();

			foreach (var (key, tracker) in _activeTouches)
			{
				tracker.HoldTime += (float)delta;

				if (tracker.HoldTime < HoldDurationSeconds)
				{
					continue;
				}

				TryClaimSeat(tracker.Position);
				completedTouches.Add(key);
			}

			foreach (var identifier in completedTouches)
			{
				_activeTouches.Remove(identifier);
			}
		}

		private void HandleScreenTouch(InputEventScreenTouch screenTouch)
		{
			if (screenTouch.Pressed)
			{
				_activeTouches[screenTouch.Index] = new TouchTracker(screenTouch.Position);
				return;
			}

			_activeTouches.Remove(screenTouch.Index);
		}

		private void HandleScreenDrag(InputEventScreenDrag screenDrag)
		{
			if (_activeTouches.TryGetValue(screenDrag.Index, out var tracker))
			{
				tracker.Position = screenDrag.Position;
			}
		}

		private void HandleMouseButton(InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex != MouseButton.Left)
			{
				return;
			}

			if (mouseButton.Pressed)
			{
				_activeTouches[MouseTouchKey] = new TouchTracker(mouseButton.Position);
				return;
			}

			_activeTouches.Remove(MouseTouchKey);
		}

		private void UpdateMousePosition(InputEventMouseMotion mouseMotion)
		{
			if (IsUiInteraction(mouseMotion.Position))
			{
				return;
			}

			if (_activeTouches.TryGetValue(MouseTouchKey, out var tracker))
			{
				tracker.Position = mouseMotion.Position;
			}
		}

		private bool TryClaimSeat(Vector2 anchorPoint)
		{
			var viewportRect = GetViewport().GetVisibleRect();
			var distance = LobbySeatPlanner.GetDistanceToNearestEdge(anchorPoint, viewportRect);

			if (distance > EdgeJoinMargin)
			{
				UpdateStatusMessage("Move closer to any edge to claim a seat.");
				return false;
			}

			var edge = LobbySeatPlanner.GetNearestEdge(anchorPoint, viewportRect);
			var initialSeatZone = LobbySeatPlanner.CreateSeatZone(edge, viewportRect, HudStripThickness, HudStripLength, anchorPoint);
			var seatItems = BuildSeatAssignmentItems(edge, initialSeatZone);
			var desiredCenters = seatItems.Select(item => item.DesiredCenter).ToList();

			if (!LobbySeatPlanner.TryArrangeSeatCenters(edge, viewportRect, HudStripLength, MaxSeatShiftFraction, desiredCenters, out var arrangedCenters))
			{
				UpdateStatusMessage("Not enough space on this edge.");
				return false;
			}

			PlayerProfile? newProfile = null;

			for (var index = 0; index < seatItems.Count; index++)
			{
				var arrangedZone = LobbySeatPlanner.CreateSeatZoneFromAxisCenter(
					edge,
					viewportRect,
					HudStripThickness,
					HudStripLength,
					arrangedCenters[index]);

				var item = seatItems[index];

				if (item.Player != null)
				{
					item.Player.Seat = arrangedZone;
					UpdateSeatIndicator(item.Player);
					UpdateCustomizationHud(item.Player);
				}
				else
				{
					newProfile = CreatePlayerProfile(arrangedZone);
				}
			}

			if (newProfile == null)
			{
				UpdateStatusMessage("Unable to register seat.");
				return false;
			}

			_sessionState.PlayerProfiles.Add(newProfile);
			CreateSeatIndicator(newProfile);
			CreateCustomizationHud(newProfile);

			RefreshPlayerDisplay();
			UpdateInputRouterSession();
			UpdateStatusMessage($"Seat claimed near the {edge.ToString().ToLower()} edge.");
			RefreshStartButtonState();

			return true;
		}

		private bool IsUiInteraction(Vector2 position)
		{
			foreach (var hud in _customizationHuds.Values)
			{
				if (!hud.Visible)
				{
					continue;
				}

				if (hud.ContainsGlobalPoint(position))
				{
					return true;
				}
			}

			return false;
		}

		private PlayerProfile CreatePlayerProfile(SeatZone seatZone)
		{
			var playerIndex = _sessionState.PlayerProfiles.Count;
			var color = _playerPalette[playerIndex % _playerPalette.Length];

			return new PlayerProfile
			{
				PlayerId = Guid.NewGuid(),
				DisplayName = $"Player {playerIndex + 1}",
				DisplayColor = color,
				Seat = seatZone,
				IsGameMaster = false
			};
		}

		private void RefreshPlayerDisplay()
		{
			if (_playerListLabel == null)
			{
				return;
			}

			if (_sessionState.PlayerProfiles.Count == 0)
			{
				_playerListLabel.Text = "No players joined yet.";
				RefreshStartButtonState();
				return;
			}

			var builder = new StringBuilder();
			builder.AppendLine("Players:");

			for (var index = 0; index < _sessionState.PlayerProfiles.Count; index++)
			{
				var profile = _sessionState.PlayerProfiles[index];
				var edgeName = profile.Seat?.Edge.ToString() ?? "Unknown";
				var anchor = profile.Seat?.AnchorPoint ?? Vector2.Zero;
				var region = profile.Seat?.ScreenRegion ?? new Rect2();
				var colorLabel = profile.DisplayColor.HasValue
					? profile.DisplayColor.Value.ToHtml(false)
					: "none";
				var avatarLabel = GetAvatarName(profile);
				builder.AppendLine(
					$"{index + 1}. {profile.DisplayName} ({edgeName}) @ ({anchor.X:0}, {anchor.Y:0}) " +
					$"color={colorLabel} avatar={avatarLabel} region=({region.Position.X:0}, {region.Position.Y:0}) size=({region.Size.X:0}, {region.Size.Y:0})");
			}

			_playerListLabel.Text = builder.ToString();
			RefreshStartButtonState();
		}

		private void UpdateStatusMessage(string message)
		{
			if (_statusLabel != null)
			{
				_statusLabel.Text = message;
			}
		}

		private void UpdateInputRouterSession()
		{
			if (_inputRouter != null)
			{
				_inputRouter.Session = _sessionState;
			}

			RefreshModuleAvailability();
		}

		private void LoadModuleCatalog()
		{
			var modulesRoot = ResolveModulesRoot();
			_moduleSelectionModel.SetModules(_moduleLoader.LoadModules(modulesRoot));
			PopulateModuleList();
		}

		private void RefreshStartButtonState()
		{
			if (_startGameButton == null)
			{
				return;
			}

			var canStart = CanStartGame(out var tooltip);
			_startGameButton.Disabled = !canStart;
			_startGameButton.TooltipText = tooltip ?? string.Empty;
		}

		private bool CanStartGame(out string message)
		{
			var module = _sessionState.SelectedModule;
			var playerCount = _sessionState.PlayerProfiles.Count;

			if (module is null)
			{
				message = "Select a module to enable the runtime.";
				return false;
			}

			if (playerCount == 0)
			{
				message = "At least one player must join before starting.";
				return false;
			}

			if (!module.SupportsPlayerCount(playerCount))
			{
				message = module.MinPlayers == module.MaxPlayers
					? $"Requires exactly {module.MinPlayers} players."
					: $"Requires {module.MinPlayers}–{module.MaxPlayers} players.";
				return false;
			}

			message = "Launch the selected module with the current table configuration.";
			return true;
		}

		private void OnStartGamePressed()
		{
			if (!CanStartGame(out var reason))
			{
				UpdateStatusMessage(reason);
				RefreshStartButtonState();
				return;
			}

			SessionTransfer.Store(_sessionState);
			var changeResult = GetTree().ChangeSceneToFile(RuntimeScenePath);

			if (changeResult != Error.Ok)
			{
				UpdateStatusMessage("Unable to load the runtime scene. Check the project configuration.");
			}
		}

		private string ResolveModulesRoot()
		{
			if (string.IsNullOrWhiteSpace(ModulesDirectory))
			{
				return string.Empty;
			}

			if (ModulesDirectory.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ||
				ModulesDirectory.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					return ProjectSettings.GlobalizePath(ModulesDirectory);
				}
				catch
				{
					return ModulesDirectory;
				}
			}

			return ModulesDirectory;
		}

		private void PopulateModuleList()
		{
			if (_moduleList == null)
			{
				return;
			}

			_moduleList.Clear();
			var modules = _moduleSelectionModel.Modules;

			if (_moduleEmptyState != null)
			{
				_moduleEmptyState.Visible = modules.Count == 0;
			}

			if (modules.Count == 0)
			{
				UpdateModuleDetails(null);
				return;
			}

			for (var index = 0; index < modules.Count; index++)
			{
				var descriptor = modules[index];
				var playerLabel = descriptor.MinPlayers == descriptor.MaxPlayers
					? $"{descriptor.MinPlayers} players"
					: $"{descriptor.MinPlayers}–{descriptor.MaxPlayers} players";
				_moduleList.AddItem($"{descriptor.DisplayName} ({playerLabel})");
			}

			var selected = _moduleSelectionModel.SelectedModule;
			var selectedIndex = -1;

			if (selected != null)
			{
				for (var index = 0; index < modules.Count; index++)
				{
					if (string.Equals(modules[index].ModuleId, selected.ModuleId, StringComparison.OrdinalIgnoreCase))
					{
						selectedIndex = index;
						break;
					}
				}
			}

			if (selectedIndex < 0 && modules.Count > 0)
			{
				selectedIndex = 0;
				_moduleSelectionModel.SelectModuleByIndex(selectedIndex);
			}

			if (selectedIndex >= 0)
			{
				_moduleList.Select(selectedIndex);
				UpdateModuleDetails(modules[selectedIndex]);
			}
			else
			{
				UpdateModuleDetails(null);
			}

			RefreshModuleAvailability();
		}

		private void UpdateModuleDetails(ModuleDescriptor? descriptor)
		{
			_sessionState.SelectedModule = descriptor;

			if (_moduleNameLabel != null)
			{
				_moduleNameLabel.Text = descriptor?.DisplayName ?? "No module selected";
			}

			if (_modulePlayersLabel != null)
			{
				_modulePlayersLabel.Text = descriptor is null
					? "Players: –"
					: $"Players: {descriptor.MinPlayers} – {descriptor.MaxPlayers}";
			}

			if (_moduleSummaryLabel != null)
			{
				_moduleSummaryLabel.Text = descriptor?.Summary ??
					"Drop modules into the Modules/ directory to enable game selection.";
			}

			if (_moduleIcon != null)
			{
				_moduleIcon.Texture = LoadModuleIcon(descriptor);
				_moduleIcon.Visible = _moduleIcon.Texture != null;
			}

			if (descriptor != null)
			{
				UpdateStatusMessage($"Selected module: {descriptor.DisplayName}");
			}
			else
			{
				UpdateStatusMessage("No module selected.");
			}

			RefreshStartButtonState();
		}

		private Texture2D? LoadModuleIcon(ModuleDescriptor? descriptor)
		{
			if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.IconPath))
			{
				return null;
			}

			var absolutePath = Path.Combine(descriptor.ModulePath, descriptor.IconPath.Replace('/', Path.DirectorySeparatorChar));
			var resourcePath = NormalizeResourcePath(absolutePath);

			try
			{
				return ResourceLoader.Exists(resourcePath)
					? ResourceLoader.Load<Texture2D>(resourcePath)
					: null;
			}
			catch
			{
				return null;
			}
		}

		private static string NormalizeResourcePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return path;
			}

			if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ||
				path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
			{
				return path;
			}

			try
			{
				return ProjectSettings.LocalizePath(path);
			}
			catch
			{
				return path;
			}
		}

		private void OnModuleListItemSelected(long index)
		{
			if (_moduleSelectionModel.SelectModuleByIndex((int)index))
			{
				UpdateModuleDetails(_moduleSelectionModel.SelectedModule);
			}
		}

		private void RefreshModuleAvailability()
		{
			if (_moduleList == null)
			{
				return;
			}

			if (_moduleEmptyState != null)
			{
				_moduleEmptyState.Visible = _moduleSelectionModel.Modules.Count == 0;
			}

			RefreshStartButtonState();
		}

		private List<SeatAssignmentItem> BuildSeatAssignmentItems(TableEdge edge, SeatZone newSeatZone)
		{
			var items = new List<SeatAssignmentItem>();

			foreach (var profile in GetPlayersOnEdge(edge))
			{
				if (profile.Seat == null)
				{
					continue;
				}

				items.Add(new SeatAssignmentItem
				{
					Player = profile,
					DesiredCenter = GetAxisCenter(profile.Seat, edge)
				});
			}

			items.Add(new SeatAssignmentItem
			{
				Player = null,
				DesiredCenter = GetAxisCenter(newSeatZone, edge)
			});

			return items;
		}

		private IEnumerable<PlayerProfile> GetPlayersOnEdge(TableEdge edge)
		{
			return _sessionState.PlayerProfiles.Where(profile => profile.Seat?.Edge == edge);
		}

		private void CreateSeatIndicator(PlayerProfile profile)
		{
			if (_seatOverlayRoot == null || profile.Seat == null)
			{
				return;
			}

			if (_seatIndicators.ContainsKey(profile.PlayerId))
			{
				UpdateSeatIndicator(profile);
				return;
			}

			if (_seatIndicatorScene?.Instantiate() is not SeatIndicatorView view)
			{
				return;
			}

			var indicatorSize = view.TemplateSize;
			var (center, rotation) = GetIndicatorPlacement(profile.Seat, indicatorSize);
			var localCenter = _seatOverlayRoot.GetCanvasTransform().AffineInverse() * center;
			view.Size = indicatorSize;
			view.PivotOffset = indicatorSize / 2f;
			view.Position = localCenter - view.PivotOffset;
			view.RotationDegrees = rotation;
			view.Configure(GetDisplayName(profile), GetIndicatorColor(profile));
			view.ZIndex = 200;
			view.ZAsRelative = false;
			_seatOverlayRoot.AddChild(view);
			_seatIndicators[profile.PlayerId] = new SeatIndicatorElements(view);
		}

		private void UpdateSeatIndicator(PlayerProfile profile)
		{
			if (profile.Seat == null)
			{
				return;
			}

			if (!_seatIndicators.TryGetValue(profile.PlayerId, out var elements))
			{
				CreateSeatIndicator(profile);
				return;
			}

			var indicatorSize = elements.View.TemplateSize;
			var (center, rotation) = GetIndicatorPlacement(profile.Seat, indicatorSize);
			var localCenter = _seatOverlayRoot!.GetCanvasTransform().AffineInverse() * center;
			elements.View.Size = indicatorSize;
			elements.View.PivotOffset = indicatorSize / 2f;
			elements.View.Position = localCenter - elements.View.PivotOffset;
			elements.View.RotationDegrees = rotation;
			elements.View.Configure(GetDisplayName(profile), GetIndicatorColor(profile));
			elements.View.ZIndex = 200;
			elements.View.ZAsRelative = false;
		}

		private void CreateCustomizationHud(PlayerProfile profile)
		{
			if (profile.Seat == null || _playerHudRoot == null)
			{
				return;
			}

			if (_completedCustomizations.Contains(profile.PlayerId))
			{
				return;
			}

			if (_customizationHuds.ContainsKey(profile.PlayerId))
			{
				UpdateCustomizationHud(profile);
				return;
			}

			if (_customizationHudScene is null)
			{
				GD.PushWarning("Player customization HUD scene could not be loaded.");
				return;
			}

			var hud = _customizationHudScene.Instantiate<PlayerCustomizationHud>();
			hud.Initialize(profile, _playerPalette, _avatarOptions);
			hud.ProfileChanged += OnPlayerProfileChanged;
			hud.CustomizationCompleted += OnCustomizationCompleted;
			hud.CustomizationCancelled += OnCustomizationCancelled;
			_playerHudRoot.AddChild(hud);
			var needsWait = !PlayerCustomizationHud.CanDisplayInSeat(profile.Seat);
			hud.SetWaitMode(needsWait);
			hud.ApplySeatZone(profile.Seat);
			_customizationHuds[profile.PlayerId] = hud;
			ResolveHudOverlaps();
		}

		private void UpdateCustomizationHud(PlayerProfile profile)
		{
			if (profile.Seat == null)
			{
				return;
			}

			if (!_customizationHuds.TryGetValue(profile.PlayerId, out var hud))
			{
				CreateCustomizationHud(profile);
				return;
			}

			var needsWait = !PlayerCustomizationHud.CanDisplayInSeat(profile.Seat);
			hud.SetWaitMode(needsWait);
			hud.ApplySeatZone(profile.Seat);
			ResolveHudOverlaps();
		}

		private void OnPlayerProfileChanged(PlayerProfile profile)
		{
			UpdateSeatIndicator(profile);
			UpdateCustomizationHud(profile);
			RefreshPlayerDisplay();
		}

		private void OnCustomizationCompleted(PlayerProfile profile)
		{
			DisposeCustomizationHud(profile.PlayerId);
			_completedCustomizations.Add(profile.PlayerId);
			UpdateSeatIndicator(profile);
			RefreshPlayerDisplay();
			ResolveHudOverlaps();
			RefreshStartButtonState();
		}

		private void OnCustomizationCancelled(PlayerProfile profile)
		{
			var playerId = profile.PlayerId;
			DisposeCustomizationHud(playerId);
			RemoveSeatIndicator(playerId);

			if (!PlayerRoster.RemovePlayer(_sessionState, playerId, _completedCustomizations, out var removedProfile))
			{
				return;
			}

			UpdateInputRouterSession();
			RefreshPlayerDisplay();
			ResolveHudOverlaps();
			RefreshStartButtonState();

			var seatEdge = removedProfile?.Seat?.Edge.ToString().ToLowerInvariant() ?? "table";
			UpdateStatusMessage($"Cancelled player slot near the {seatEdge} edge.");
		}

		private void DisposeCustomizationHud(Guid playerId)
		{
			if (!_customizationHuds.TryGetValue(playerId, out var hud))
			{
				return;
			}

			hud.ProfileChanged -= OnPlayerProfileChanged;
			hud.CustomizationCompleted -= OnCustomizationCompleted;
			hud.CustomizationCancelled -= OnCustomizationCancelled;
			_customizationHuds.Remove(playerId);
			hud.QueueFree();
		}

		private void RemoveSeatIndicator(Guid playerId)
		{
			if (!_seatIndicators.TryGetValue(playerId, out var elements))
			{
				return;
			}

			_seatIndicators.Remove(playerId);
			elements.View.QueueFree();
		}

		private static string GetDisplayName(PlayerProfile profile)
		{
			return string.IsNullOrWhiteSpace(profile.DisplayName) ? "Player" : profile.DisplayName!;
		}

		private Color GetIndicatorColor(PlayerProfile profile)
		{
			var baseColor = profile.DisplayColor ?? new Color(0.8f, 0.8f, 0.8f);
			baseColor.A = 0.7f;
			return baseColor;
		}

		private string GetAvatarName(PlayerProfile profile)
		{
			if (profile.Avatar is null)
			{
				return "default";
			}

			foreach (var option in _avatarOptions)
			{
				if (ReferenceEquals(option.Texture, profile.Avatar))
				{
					return option.Id;
				}
			}

			return "custom";
		}

		private Rect2 ComputeIndicatorRect(SeatZone seatZone)
		{
			var maxThickness = seatZone.Edge is TableEdge.Top or TableEdge.Bottom
				? Mathf.Max(1f, seatZone.ScreenRegion.Size.Y)
				: Mathf.Max(1f, seatZone.ScreenRegion.Size.X);
			var thickness = Mathf.Clamp(SeatIndicatorThickness, 1f, maxThickness);
			thickness = Mathf.Max(thickness, 28f);

			return seatZone.Edge switch
			{
				TableEdge.Bottom => new Rect2(
					seatZone.ScreenRegion.Position.X,
					seatZone.ScreenRegion.Position.Y + seatZone.ScreenRegion.Size.Y - thickness,
					seatZone.ScreenRegion.Size.X,
					thickness),
				TableEdge.Top => new Rect2(
					seatZone.ScreenRegion.Position.X,
					seatZone.ScreenRegion.Position.Y,
					seatZone.ScreenRegion.Size.X,
					thickness),
				TableEdge.Left => new Rect2(
					seatZone.ScreenRegion.Position.X,
					seatZone.ScreenRegion.Position.Y,
					thickness,
					seatZone.ScreenRegion.Size.Y),
				TableEdge.Right => new Rect2(
					seatZone.ScreenRegion.Position.X + seatZone.ScreenRegion.Size.X - thickness,
					seatZone.ScreenRegion.Position.Y,
					thickness,
					seatZone.ScreenRegion.Size.Y),
				_ => new Rect2()
			};
		}

		private static float GetAxisCenter(SeatZone seatZone, TableEdge edge)
		{
			return edge switch
			{
				TableEdge.Bottom or TableEdge.Top => seatZone.ScreenRegion.Position.X + (seatZone.ScreenRegion.Size.X / 2f),
				TableEdge.Left or TableEdge.Right => seatZone.ScreenRegion.Position.Y + (seatZone.ScreenRegion.Size.Y / 2f),
				_ => 0f
			};
		}

		private static AvatarOption[] CreateDefaultAvatarOptions()
		{
			return new[]
			{
				new AvatarOption("Sun", CreateAvatarTexture(Color.FromHtml("F8961E"), Color.FromHtml("F9C74F"))),
				new AvatarOption("Lagoon", CreateAvatarTexture(Color.FromHtml("43AA8B"), Color.FromHtml("90BE6D"))),
				new AvatarOption("Twilight", CreateAvatarTexture(Color.FromHtml("577590"), Color.FromHtml("8ECAE6"))),
				new AvatarOption("Rose", CreateAvatarTexture(Color.FromHtml("F94144"), Color.FromHtml("FB8B24")))
			};
		}

		private void ResolveHudOverlaps()
		{
			var huds = _customizationHuds.Values.ToList();

			foreach (var hud in huds)
			{
				if (hud.CurrentSeat is null)
				{
					continue;
				}

				if (!hud.IsWaiting && !PlayerCustomizationHud.CanDisplayInSeat(hud.CurrentSeat))
				{
					hud.SetWaitMode(true);
				}
				else if (hud.IsWaiting && PlayerCustomizationHud.CanDisplayInSeat(hud.CurrentSeat))
				{
					hud.SetWaitMode(false);
				}
			}

			huds = _customizationHuds.Values.ToList();

			for (var i = 0; i < huds.Count; i++)
			{
				var hud = huds[i];

				if (hud.IsWaiting || hud.CurrentSeat is null)
				{
					continue;
				}

				var rect = hud.GetGlobalRect();

				for (var j = 0; j < huds.Count; j++)
				{
					if (i == j)
					{
						continue;
					}

					var other = huds[j];

					if (other.IsWaiting || other.CurrentSeat is null)
					{
						continue;
					}

					if (rect.Intersects(other.GetGlobalRect()))
					{
						hud.SetWaitMode(true);
						break;
					}
				}
			}
		}

		private sealed class SeatIndicatorElements
		{
			public SeatIndicatorElements(SeatIndicatorView view)
			{
				View = view;
			}

			public SeatIndicatorView View { get; }
		}

		private (Vector2 center, float rotationDegrees) GetIndicatorPlacement(SeatZone seatZone, Vector2 indicatorSize)
		{
			const float margin = 0f;
			var rect = seatZone.ScreenRegion;
			var rotation = seatZone.RotationDegrees;
			Vector2 basePoint;
			Vector2 normal;
			var thickness = indicatorSize.Y;

			switch (seatZone.Edge)
			{
				case TableEdge.Bottom:
					basePoint = new Vector2(rect.Position.X + rect.Size.X / 2f, rect.Position.Y + rect.Size.Y);
					normal = Vector2.Up;
					thickness = indicatorSize.Y;
					break;
				case TableEdge.Top:
					basePoint = new Vector2(rect.Position.X + rect.Size.X / 2f, rect.Position.Y);
					normal = Vector2.Down;
					thickness = indicatorSize.Y;
					break;
				case TableEdge.Left:
					basePoint = new Vector2(rect.Position.X, rect.Position.Y + rect.Size.Y / 2f);
					normal = Vector2.Right;
					break;
				case TableEdge.Right:
					basePoint = new Vector2(rect.Position.X + rect.Size.X, rect.Position.Y + rect.Size.Y / 2f);
					normal = Vector2.Left;
					break;
				default:
					basePoint = rect.GetCenter();
					normal = Vector2.Zero;
					thickness = indicatorSize.Y;
					break;
			}

			var offset = (thickness / 2f) + margin;
			var center = basePoint + normal * offset;
			return (center, rotation);
		}

		private static Texture2D CreateAvatarTexture(Color topColor, Color bottomColor)
		{
			const int size = 64;
			var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);

			for (var y = 0; y < size; y++)
			{
				var rowColor = y < size / 2 ? topColor : bottomColor;
				for (var x = 0; x < size; x++)
				{
					image.SetPixel(x, y, rowColor);
				}
			}

			return ImageTexture.CreateFromImage(image);
		}

		private sealed class TouchTracker
		{
			public TouchTracker(Vector2 position)
			{
				Position = position;
				HoldTime = 0f;
			}

			public Vector2 Position { get; set; }
			public float HoldTime { get; set; }
		}

		private sealed class SeatAssignmentItem
		{
			public PlayerProfile? Player { get; init; }
			public float DesiredCenter { get; init; }
		}
	}
}
