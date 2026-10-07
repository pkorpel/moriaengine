/*
    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
    Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.IO;
using Microsoft.Build.Framework;

namespace SteamEngine.Common {
	public class MsBuildLogger : INodeLogger {
		#region INodeLogger implementation

		public virtual void Initialize(IEventSource eventSource, int nodeCount) {
			this.Initialize(eventSource);
		}

		#endregion

		#region ILogger implementation

		public virtual void Initialize(IEventSource eventSource) {
			eventSource.BuildStarted += this.BuildStartedHandler;
			eventSource.BuildFinished += this.BuildFinishedHandler;

			eventSource.ErrorRaised += this.ErrorHandler;
			eventSource.WarningRaised += this.WarningHandler;

			eventSource.MessageRaised += this.MessageHandler;

			// project is the level that barely makes sense to display, but I don't really know what it means :)
			//eventSource.ProjectFinished += this.ProjectFinishedHandler;
			//eventSource.ProjectStarted += this.ProjectStartedHandler;
			// target is too detailed
			//eventSource.TargetFinished += this.TargetFinishedHandler;
			//eventSource.TargetStarted += this.TargetStartedHandler;
			// task is WAY too detailed
			//eventSource.TaskFinished += this.TaskFinishedHandler;
			//eventSource.TaskStarted += this.TaskStartedHandler;
		}

		public virtual void Shutdown() {

		}

		public string Parameters { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

		public LoggerVerbosity Verbosity {
			get { return LoggerVerbosity.Normal; }
			set { throw new NotImplementedException(); }
		}

		#endregion

		public void BuildFinishedHandler(object sender, BuildFinishedEventArgs e) {
			Logger.StaticWriteLine(e.Message);
		}

		public void BuildStartedHandler(object sender, BuildStartedEventArgs e) {
			Logger.StaticWriteLine(e.Message);
		}

		private static string GetFullPath(string fileName, string projectPath)
		{
			var path = Path.GetDirectoryName(projectPath);
			return Path.Combine(path, fileName);
		}

		public void ErrorHandler(object sender, BuildErrorEventArgs e) {
			Logger.WriteError(file: GetFullPath(e.File, e.ProjectFile), line: e.LineNumber, data: $": {e.Subcategory} {e.Code}: {e.Message}");
		}

		public void MessageHandler(object sender, BuildMessageEventArgs e) {
			//if (e.Importance == MessageImportance.High) {
			//	Logger.WriteDebug(e.Message);
			//}
		}

		public void WarningHandler(object sender, BuildWarningEventArgs e) {
			Logger.WriteWarning(file: GetFullPath(e.File, e.ProjectFile), line: e.LineNumber, data: $": {e.Subcategory} {e.Code}: {e.Message}");
		}
	}
}