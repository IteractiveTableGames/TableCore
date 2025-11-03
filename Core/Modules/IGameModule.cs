using TableCore.Core;

namespace TableCore.Core.Modules
{
    /// <summary>
    /// Contract implemented by the root node of every runtime module scene.
    /// </summary>
    public interface IGameModule
    {
        /// <summary>
        /// Called immediately after the module scene is instantiated.
        /// </summary>
        /// <param name="session">Active session snapshot describing players and configuration.</param>
        /// <param name="services">Framework-provided services available to the module.</param>
        void Initialize(SessionState session, IModuleServices services);

        /// <summary>
        /// Optional per-frame update hook invoked by the runtime when enabled.
        /// </summary>
        /// <param name="delta">Elapsed time in seconds since the previous frame.</param>
        void Tick(double delta);

        /// <summary>
        /// Called when the runtime is about to unload the module.
        /// </summary>
        void Shutdown();
    }
}
