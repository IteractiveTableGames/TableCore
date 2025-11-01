using System;
using System.Collections.Generic;
using System.Linq;
using TableCore.Core;

namespace TableCore.Lobby
{
    /// <summary>
    /// Maintains the collection of discovered modules and the current selection for the lobby.
    /// </summary>
    public class ModuleSelectionModel
    {
        private readonly SessionState _sessionState;
        private readonly List<ModuleDescriptor> _modules = new();

        public ModuleSelectionModel(SessionState sessionState)
        {
            _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        }

        public IReadOnlyList<ModuleDescriptor> Modules => _modules;

        public ModuleDescriptor? SelectedModule => _sessionState.SelectedModule;

        public void SetModules(IEnumerable<ModuleDescriptor> modules)
        {
            _modules.Clear();

            if (modules is null)
            {
                _sessionState.SelectedModule = null;
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var module in modules)
            {
                if (module is null)
                {
                    continue;
                }

                if (!seen.Add(module.ModuleId))
                {
                    continue;
                }

                _modules.Add(module);
            }

            ModuleDescriptor? selected = null;

            if (_sessionState.SelectedModule is not null)
            {
                selected = _modules.FirstOrDefault(m => AreSameModule(m, _sessionState.SelectedModule));
            }

            if (selected is null && _modules.Count > 0)
            {
                selected = _modules[0];
            }

            _sessionState.SelectedModule = selected;
        }

        public bool SelectModuleByIndex(int index)
        {
            if (index < 0 || index >= _modules.Count)
            {
                return false;
            }

            _sessionState.SelectedModule = _modules[index];
            return true;
        }

        public bool SelectModule(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId))
            {
                return false;
            }

            var module = _modules.FirstOrDefault(m =>
                string.Equals(m.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase));

            if (module is null)
            {
                return false;
            }

            _sessionState.SelectedModule = module;
            return true;
        }

        private static bool AreSameModule(ModuleDescriptor a, ModuleDescriptor b)
        {
            if (!string.IsNullOrWhiteSpace(a.ModuleId) && !string.IsNullOrWhiteSpace(b.ModuleId))
            {
                return string.Equals(a.ModuleId, b.ModuleId, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
