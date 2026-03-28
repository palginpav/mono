//
// ExtendedProtectionPolicy.cs
//
// Implements Extended Protection for Authentication.
// Windows System.ServiceModel.dll HttpTransportBindingElement accesses
// PolicyEnforcement, ProtectionScenario and other properties.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Security.Authentication.ExtendedProtection
{
	[Serializable]
	[TypeConverter (typeof (ExtendedProtectionPolicyTypeConverter))]
	public class ExtendedProtectionPolicy : ISerializable
	{
		PolicyEnforcement _policyEnforcement;
		ProtectionScenario _protectionScenario;
		ServiceNameCollection _customServiceNames;
		ChannelBinding _customChannelBinding;

		public ExtendedProtectionPolicy (PolicyEnforcement policyEnforcement)
		{
			_policyEnforcement = policyEnforcement;
			_protectionScenario = ProtectionScenario.TransportSelected;
		}

		public ExtendedProtectionPolicy (PolicyEnforcement policyEnforcement, ChannelBinding customChannelBinding)
		{
			_policyEnforcement = policyEnforcement;
			_customChannelBinding = customChannelBinding;
			_protectionScenario = ProtectionScenario.TransportSelected;
		}

		public ExtendedProtectionPolicy (PolicyEnforcement policyEnforcement, ProtectionScenario protectionScenario, ICollection customServiceNames)
		{
			_policyEnforcement = policyEnforcement;
			_protectionScenario = protectionScenario;
			_customServiceNames = customServiceNames != null ? new ServiceNameCollection (customServiceNames) : null;
		}

		public ExtendedProtectionPolicy (PolicyEnforcement policyEnforcement, ProtectionScenario protectionScenario, ServiceNameCollection customServiceNames)
		{
			_policyEnforcement = policyEnforcement;
			_protectionScenario = protectionScenario;
			_customServiceNames = customServiceNames;
		}

		protected ExtendedProtectionPolicy (SerializationInfo info, StreamingContext context)
		{
			_policyEnforcement = (PolicyEnforcement) info.GetInt32 ("PolicyEnforcement");
			_protectionScenario = (ProtectionScenario) info.GetInt32 ("ProtectionScenario");
		}

		public ChannelBinding CustomChannelBinding {
			get { return _customChannelBinding; }
		}

		public ServiceNameCollection CustomServiceNames {
			get { return _customServiceNames; }
		}

		public static bool OSSupportsExtendedProtection {
			get { return false; }
		}

		public PolicyEnforcement PolicyEnforcement {
			get { return _policyEnforcement; }
		}

		public ProtectionScenario ProtectionScenario {
			get { return _protectionScenario; }
		}

		public override string ToString ()
		{
			return _policyEnforcement.ToString ();
		}

		[SecurityPermission (SecurityAction.LinkDemand, SerializationFormatter = true)]
		void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context)
		{
			info.AddValue ("PolicyEnforcement", (int) _policyEnforcement);
			info.AddValue ("ProtectionScenario", (int) _protectionScenario);
		}
	}
}
