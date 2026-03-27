namespace System
{
	using System.Runtime.CompilerServices;

	[FriendAccessAllowed]
	internal static class CompatibilitySwitches
	{
		private static bool s_AreSwitchesSet;
		private static bool s_isNetFx40TimeSpanLegacyFormatMode;
		private static bool s_isNetFx40LegacySecurityPolicy;
		private static bool s_isNetFx45LegacyManagedDeflateStream;

		public static bool IsCompatibilityBehaviorDefined
		{
			get { return s_AreSwitchesSet; }
		}

		private static bool IsCompatibilitySwitchSet(string compatibilitySwitch)
		{
			bool? result = AppDomain.CurrentDomain.IsCompatibilitySwitchSet(compatibilitySwitch);
			return (result.HasValue && result.Value);
		}

		internal static void InitializeSwitches()
		{
			s_isNetFx40TimeSpanLegacyFormatMode = IsCompatibilitySwitchSet("NetFx40_TimeSpanLegacyFormatMode");
			s_isNetFx40LegacySecurityPolicy = IsCompatibilitySwitchSet("NetFx40_LegacySecurityPolicy");
			s_isNetFx45LegacyManagedDeflateStream = IsCompatibilitySwitchSet("NetFx45_LegacyManagedDeflateStream");
			s_AreSwitchesSet = true;
		}

		public static bool IsAppEarlierThanSilverlight4
		{
			get { return false; }
		}

		public static bool IsAppEarlierThanWindowsPhone8
		{
			get { return false; }
		}

		public static bool IsAppEarlierThanWindowsPhoneMango
		{
			get { return false; }
		}

		public static bool IsNetFx40TimeSpanLegacyFormatMode
		{
			get { return s_isNetFx40TimeSpanLegacyFormatMode; }
		}

		public static bool IsNetFx40LegacySecurityPolicy
		{
			get { return s_isNetFx40LegacySecurityPolicy; }
		}

		public static bool IsNetFx45LegacyManagedDeflateStream
		{
			get { return s_isNetFx45LegacyManagedDeflateStream; }
		}
	}
}
