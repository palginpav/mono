// ClientConfigurationHost — minimal config host for Windows compat
//
// Windows ConfigSystem creates ClientConfigurationHost via reflection.
// This wraps InternalConfigHost and provides client config paths
// using ClientConfigPaths from referencesource.

using System.Configuration.Internal;

namespace System.Configuration
{
	internal class ClientConfigurationHost : DelegatingConfigHost, IInternalConfigClientHost
	{
		private InternalConfigHost _host;
		private static ClientConfigPaths _configPaths;

		public ClientConfigurationHost ()
		{
			_host = new InternalConfigHost ();
			Host = _host;
		}

		public override void Init (IInternalConfigRoot configRoot, params object[] hostInitParams)
		{
			// Base init
			try {
				_host.Init (configRoot, hostInitParams);
			} catch {
				// Ignore init errors — config paths may not be available
			}
		}

		string IInternalConfigClientHost.GetExeConfigPath ()
		{
			if (_configPaths == null)
				_configPaths = ClientConfigPaths.Current;
			return _configPaths != null ? _configPaths.ApplicationConfigUri : null;
		}

		string IInternalConfigClientHost.GetRoamingUserConfigPath ()
		{
			if (_configPaths == null)
				_configPaths = ClientConfigPaths.Current;
			return _configPaths != null ? _configPaths.RoamingConfigFilename : null;
		}

		string IInternalConfigClientHost.GetLocalUserConfigPath ()
		{
			if (_configPaths == null)
				_configPaths = ClientConfigPaths.Current;
			return _configPaths != null ? _configPaths.LocalConfigFilename : null;
		}

		bool IInternalConfigClientHost.IsExeConfig (string configPath)
		{
			return string.Equals (configPath, "MACHINE/EXE",
				StringComparison.OrdinalIgnoreCase);
		}

		bool IInternalConfigClientHost.IsRoamingUserConfig (string configPath)
		{
			return string.Equals (configPath, "MACHINE/EXE/ROAMING_USER",
				StringComparison.OrdinalIgnoreCase);
		}

		bool IInternalConfigClientHost.IsLocalUserConfig (string configPath)
		{
			return string.Equals (configPath, "MACHINE/EXE/ROAMING_USER/LOCAL_USER",
				StringComparison.OrdinalIgnoreCase);
		}
	}
}
