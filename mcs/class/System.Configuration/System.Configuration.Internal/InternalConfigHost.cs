// InternalConfigHost — minimal implementation for Windows compat
//
// Windows ClientConfigurationHost wraps InternalConfigHost.
// This provides the minimum IInternalConfigHost needed for init.

using System.IO;
using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
	internal class InternalConfigHost : DelegatingConfigHost
	{
		public InternalConfigHost ()
		{
		}
	}
}
