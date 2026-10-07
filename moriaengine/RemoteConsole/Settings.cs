using System;
using System.Collections.Generic;
using System.ComponentModel;
using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public static class Settings {
		public const string iniFileName = "steamrc.ini";
		public const string epsSectionName = "endpoint";
		public const string linkSectionName = "linkByExtension";
		public const string generalSectionName = "general";

		public static bool saveEndpointPasswords = true;

		static Settings() {
			IniFile ini = new IniFile(iniFileName);

			foreach (IniFileSection section in ini.GetSections(epsSectionName)) {
				knownEndPoints.Add(LoadEPSFromIni(section));
			}
		
			IniFileSection generalSec = ini.GetNewOrParsedSection(generalSectionName);
			saveEndpointPasswords = generalSec.GetValue("saveEndpointPasswords", true, "true: Save passwords, false: Don't.");

			ini.WriteToFile();
		}

		internal static void Init() {

		}

		internal static void Save() {
			try {
				IniFile ini = new IniFile(iniFileName);

				Dictionary<string, EndPointSetting> pointsToSave = new Dictionary<string, EndPointSetting>();
				foreach (EndPointSetting eps in knownEndPoints) {
					pointsToSave.Add(eps.Name, eps);
				}

				List<IniFileSection> toRemove = new List<IniFileSection>();

				foreach (IniFileSection section in ini.GetSections(epsSectionName)) {
					string name = section.GetValue<string>("name");
					EndPointSetting eps;
					if (pointsToSave.TryGetValue(name, out eps)) {
						SaveEPSToIni(eps, section);
						pointsToSave.Remove(name);
					} else {
						toRemove.Add(section);
					}
				}

				foreach (EndPointSetting eps in pointsToSave.Values) {
					IniFileSection section = ini.GetNewSection(epsSectionName);
					SaveEPSToIni(eps, section);
				}

				foreach (IniFileSection section in toRemove) {
					ini.RemoveSection(section);
				}

				IniFileSection generalSec = ini.GetNewOrParsedSection(generalSectionName);
				generalSec.SetValue("saveEndpointPasswords", saveEndpointPasswords, "true: Save passwords, false: Don't.");

				ini.WriteToFile();
			} catch (Exception e) {
				Common.Logger.WriteError(e);
			}
		}

		private static void SaveEPSToIni(EndPointSetting eps, IniFileSection section) {
			section.SetValue("Name", eps.Name, null);
			section.SetValue("Address", eps.Address, null);
			section.SetValue("UserName", eps.UserName, null);
			if (saveEndpointPasswords) {
				section.SetValue("Password", eps.Password, null);
			} else {
				section.RemoveValue("Password");
			}
			section.SetValue("Port", eps.Port, null);
			section.SetValue("KeepReconnecting", eps.KeepReconnecting, null);
		}

		private static EndPointSetting LoadEPSFromIni(IniFileSection section) {
			EndPointSetting eps = new EndPointSetting();
			eps.Name = section.GetValue("Name", eps.Name, null);
			eps.Address = section.GetValue("Address", eps.Address, null);
			eps.UserName = section.GetValue("UserName", eps.UserName, null);
			eps.Password = section.GetValue("Password", eps.Password, null);
			if (!saveEndpointPasswords) {
				section.RemoveValue("Password");
			}
			eps.Port = section.GetValue("Port", eps.Port, null);
			eps.KeepReconnecting = section.GetValue("KeepReconnecting", eps.KeepReconnecting, null);
			return eps;
		}

		public static BindingList<EndPointSetting> knownEndPoints = new BindingList<EndPointSetting>();

		internal static bool GetCommandLineForExt(string ext, out string exePath, out string argumentsToFormat) {
			IniFile ini = new IniFile(iniFileName);

			foreach (IniFileSection section in ini.GetSections(linkSectionName)) {
				string sectionExt = section.GetValue<string>("extension");
				if (string.Equals(ext, sectionExt, StringComparison.OrdinalIgnoreCase)) {
					exePath = section.GetValue<string>("exe");
					argumentsToFormat = section.GetValue<string>("arguments");
					return true;
				}
			}

			exePath = argumentsToFormat = null;
			return false;
		}
	}

	public class EndPointSetting {
		private int port;
		private string name;
		private string address;
		private string userName;
		private string password;
		private bool keepReconnecting;

		public EndPointSetting() {
			this.port = 2594;
			this.name = "New endpoint";
			this.address = "localhost";
			this.userName = "";
			this.password = "";
			this.keepReconnecting = true;
		}

		public EndPointSetting(EndPointSetting eps) {
			this.port = eps.port;
			this.name = eps.name;
			this.address = eps.address;
			this.userName = eps.userName;
			this.password = eps.password;
			this.keepReconnecting = eps.keepReconnecting;
		}

		public string Name {
			set { this.name = value; }
			get { return this.name; }
		}
		public string Address {
			set { this.address = value; }
			get { return this.address; }
		}
		public string UserName {
			set { this.userName = value; }
			get { return this.userName; }
		}
		public string Password {
			set { this.password = value; }
			get { return this.password; }
		}

		public bool KeepReconnecting {
			set { this.keepReconnecting = value; }
			get { return this.keepReconnecting; }
		}

		public int Port {
			set {
				if (value < 0 || value > 65535) {
					throw new SEException("Port number out of range");
				}
				this.port = value;
			}
			get { return this.port; }
		}

		public override string ToString() {
			return this.name;
		}
	}
}
