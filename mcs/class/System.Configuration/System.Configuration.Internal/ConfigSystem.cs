// ConfigSystem — runtime config system orchestrator
//
// Ported from referencesource configsystem.cs.
// Creates InternalConfigRoot and the config host, initializes both.

namespace System.Configuration.Internal
{
	internal class ConfigSystem : IConfigSystem
	{
		IInternalConfigRoot _configRoot;
		IInternalConfigHost _configHost;

		void IConfigSystem.Init (Type typeConfigHost, params object[] hostInitParams)
		{
			_configRoot = new InternalConfigRoot ();
			_configHost = (IInternalConfigHost) TypeUtil.CreateInstanceWithReflectionPermission (typeConfigHost);

			_configRoot.Init (_configHost, false);
			_configHost.Init (_configRoot, hostInitParams);
		}

		IInternalConfigHost IConfigSystem.Host {
			get { return _configHost; }
		}

		IInternalConfigRoot IConfigSystem.Root {
			get { return _configRoot; }
		}
	}
}
