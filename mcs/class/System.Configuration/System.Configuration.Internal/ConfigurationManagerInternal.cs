//
// System.Configuration.Internal.ConfigurationManagerInternal
//
// Implements IConfigurationManagerInternal to provide configuration paths
// and settings for the .NET Framework configuration system.
//
// This implementation delegates to the existing mono infrastructure
// (ClientConfigPaths, ConfigurationManager) rather than pulling in the
// full referencesource ClientConfigurationHost dependency chain.
//

using System.IO;
using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
	internal sealed class ConfigurationManagerInternal : IConfigurationManagerInternal
	{
		bool IConfigurationManagerInternal.SupportsUserConfig {
			get { return ConfigurationManager.ConfigurationSystem.SupportsUserConfig; }
		}

		bool IConfigurationManagerInternal.SetConfigurationSystemInProgress {
			get { return false; }
		}

		string IConfigurationManagerInternal.MachineConfigPath {
			get {
				return Path.Combine (
					Path.Combine (RuntimeEnvironment.GetRuntimeDirectory (), "Config"),
					"machine.config");
			}
		}

		string IConfigurationManagerInternal.ApplicationConfigUri {
			get { return ClientConfigPaths.Current.ApplicationConfigUri; }
		}

		string IConfigurationManagerInternal.ExeProductName {
			get { return ClientConfigPaths.Current.ProductName; }
		}

		string IConfigurationManagerInternal.ExeProductVersion {
			get { return ClientConfigPaths.Current.ProductVersion; }
		}

		string IConfigurationManagerInternal.ExeRoamingConfigDirectory {
			get { return ClientConfigPaths.Current.RoamingConfigDirectory; }
		}

		string IConfigurationManagerInternal.ExeRoamingConfigPath {
			get { return ClientConfigPaths.Current.RoamingConfigFilename; }
		}

		string IConfigurationManagerInternal.ExeLocalConfigDirectory {
			get { return ClientConfigPaths.Current.LocalConfigDirectory; }
		}

		string IConfigurationManagerInternal.ExeLocalConfigPath {
			get { return ClientConfigPaths.Current.LocalConfigFilename; }
		}

		string IConfigurationManagerInternal.UserConfigFilename {
			get { return ClientConfigPaths.UserConfigFilename; }
		}
	}
}
