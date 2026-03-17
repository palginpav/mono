//
// ComEventsHelper.cs
//
// Authors:
//	Alexander Köplinger <alexander.koeplinger@xamarin.com>
//
// Copyright (C) 2016 Xamarin Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace System.Runtime.InteropServices
{
	public static class ComEventsHelper
	{
		static readonly object providersLock = new object ();
		static readonly ConditionalWeakTable<object, Dictionary<Type, object>> providersByRcw = new ConditionalWeakTable<object, Dictionary<Type, object>> ();

		static void Trace (string message)
		{
			if (Environment.GetEnvironmentVariable ("WINE_MONO_COMEVENT_TRACE") == "1")
				Console.Error.WriteLine ("WINE_MONO_COMEVENT_TRACE: " + message);
		}

		static Type FindEventInterface (Type declaringType, string methodName)
		{
			if (declaringType == null)
				throw new ArgumentNullException (nameof (declaringType));
			if (methodName == null)
				throw new ArgumentNullException (nameof (methodName));

			if (Attribute.IsDefined (declaringType, typeof (ComEventInterfaceAttribute), false) &&
				declaringType.GetMethod (methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
				return declaringType;

			foreach (Type iface in declaringType.GetInterfaces ()) {
				if (!Attribute.IsDefined (iface, typeof (ComEventInterfaceAttribute), false))
					continue;
				if (iface.GetMethod (methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
					return iface;
			}

			throw new InvalidCastException ("Specified cast is not valid.");
		}

		static object NormalizeRcw (object rcw)
		{
			IntPtr punk = IntPtr.Zero;

			if (rcw == null)
				throw new ArgumentNullException (nameof (rcw));

			Trace ("NormalizeRcw in type=" + rcw.GetType ().FullName + " isCom=" + Marshal.IsComObject (rcw));

			try {
				__ComObject comObject = rcw as __ComObject;
				if (comObject != null && !Marshal.IsComObject (rcw)) {
					rcw = comObject.GetTransparentProxyObject ();
					Trace ("NormalizeRcw raw rcw -> " + rcw.GetType ().FullName + " isCom=" + Marshal.IsComObject (rcw));
				}

				if (rcw is IntPtr)
					return Marshal.GetObjectForIUnknown ((IntPtr)rcw);

				punk = Marshal.GetIUnknownForObject (rcw);
				rcw = Marshal.GetObjectForIUnknown (punk);
				Trace ("NormalizeRcw out type=" + rcw.GetType ().FullName + " isCom=" + Marshal.IsComObject (rcw));
				return rcw;
			} catch (ArgumentException) {
				Trace ("NormalizeRcw ArgumentException");
				return rcw;
			} catch (InvalidCastException) {
				Trace ("NormalizeRcw InvalidCastException");
				return rcw;
			} finally {
				if (punk != IntPtr.Zero)
					Marshal.Release (punk);
			}
		}

		static object GetOrCreateEventProvider (object rcw, Type providerType)
		{
			Dictionary<Type, object> providers;
			object provider;

			rcw = NormalizeRcw (rcw);
			if (providerType == null)
				throw new ArgumentNullException (nameof (providerType));
			Trace ("GetOrCreateEventProvider rcw=" + rcw.GetType ().FullName + " provider=" + providerType.FullName);

			lock (providersLock) {
				if (!providersByRcw.TryGetValue (rcw, out providers)) {
					providers = new Dictionary<Type, object> ();
					providersByRcw.Add (rcw, providers);
				}

				if (!providers.TryGetValue (providerType, out provider)) {
					provider = Activator.CreateInstance (
						providerType,
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
						null,
						new object [] { rcw },
						null);
					providers.Add (providerType, provider);
				}

				return provider;
			}
		}

		internal static void InvokeComEventMethod (object rcw, Type declaringType, string methodName, object handler)
		{
			if (handler == null)
				throw new ArgumentNullException (nameof (handler));
			Trace ("InvokeComEventMethod declaringType=" + declaringType.FullName + " method=" + methodName + " rcw=" + rcw.GetType ().FullName);

			Type eventInterface = FindEventInterface (declaringType, methodName);
			ComEventInterfaceAttribute attr = (ComEventInterfaceAttribute)Attribute.GetCustomAttribute (
				eventInterface, typeof (ComEventInterfaceAttribute), false);
			if (attr == null)
				throw new InvalidCastException ("Specified cast is not valid.");

			object provider = GetOrCreateEventProvider (rcw, attr.EventProvider);
			MethodInfo method = attr.EventProvider.GetMethod (
				methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null)
				throw new MissingMethodException (attr.EventProvider.FullName, methodName);

			method.Invoke (provider, new object [] { handler });
		}

		[MonoTODO]
		public static void Combine(object rcw, Guid iid, int dispid, Delegate d)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public static Delegate Remove(object rcw, Guid iid, int dispid, Delegate d)
		{
			throw new NotImplementedException ();
		}
	}
}
