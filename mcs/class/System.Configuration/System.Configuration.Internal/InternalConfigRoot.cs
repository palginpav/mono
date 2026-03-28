// InternalConfigRoot — Windows .NET Framework compat implementation
//
// Windows System.Configuration.dll ConfigSystem creates InternalConfigRoot
// during initialization. This implementation wraps mono's existing config
// infrastructure (InternalConfigurationRoot) to satisfy the type requirement.

namespace System.Configuration.Internal
{
	internal sealed class InternalConfigRoot : IInternalConfigRoot
	{
		IInternalConfigHost _host;
		bool _isDesignTime;

		public void Init (IInternalConfigHost host, bool isDesignTime)
		{
			_host = host;
			_isDesignTime = isDesignTime;
		}

		public bool IsDesignTime {
			get { return _isDesignTime; }
		}

		public IInternalConfigRecord GetConfigRecord (string configPath)
		{
			return null;
		}

		public object GetSection (string section, string configPath)
		{
			return null;
		}

		public string GetUniqueConfigPath (string configPath)
		{
			return configPath;
		}

		public IInternalConfigRecord GetUniqueConfigRecord (string configPath)
		{
			return null;
		}

		public void RemoveConfig (string configPath)
		{
		}

#pragma warning disable 67
		public event InternalConfigEventHandler ConfigChanged;
		public event InternalConfigEventHandler ConfigRemoved;
#pragma warning restore 67
	}
}
