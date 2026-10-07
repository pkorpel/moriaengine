using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SteamEngine.Common;
using Buffer = SteamEngine.Communication.Buffer;

namespace SteamEngine.AuxiliaryServer.SphereServers {
	public class SphereServerConnection : Disposable {
		readonly Socket socket;
		readonly SphereServerClient state;

		Buffer receivingBuffer = Pool<Buffer>.Acquire();

		private AsyncCallback beginSendCallback;
		private AsyncCallback beginReceiveCallback;

		private SynchronizedQueue<CommandResponse> commandResponders = new SynchronizedQueue<CommandResponse>();

		delegate void ReceievingMode(TextReader reader);

		private readonly ReceievingMode nonLoggedMode;
		private readonly ReceievingMode loggedInMode;
		private readonly ReceievingMode commandReplyMode;
		private readonly ReceievingMode ignoreMode;

		private ReceievingMode receivingMode;

		private string closingReason;

		internal SphereServerConnection(Socket socket, SphereServerSetup setup) {
			this.beginSendCallback = this.BeginSendCallback;
			this.beginReceiveCallback = this.BeginReceieveCallback;

			this.nonLoggedMode = this.ModeNotLoggedIn;
			this.loggedInMode = this.ModeLoggedIn;
			this.commandReplyMode = this.ModeCommandReply;
			this.ignoreMode = this.ModeIgnore;

			this.receivingMode = this.nonLoggedMode;

			this.socket = socket;
			this.BeginReceive();

			this.state = new SphereServerClient(this, setup);
		}

		public SphereServerClient State {
			get {
				return this.state;
			}
		}

		public override string ToString() {
			return "SphereConn '" + this.state + "'";
		}

		public bool IsConnected {
			get {
				return this.socket.Connected;
			}
		}

		internal void StartLoginSequence(SphereServerSetup sphereServerSetup) {
			this.BeginSend(" ");
			this.BeginSend(sphereServerSetup.AdminAccount);

			new Timer(this.SendPassword, sphereServerSetup.AdminPassword, 1000, Timeout.Infinite);
			
		}
		private void SendPassword(object o) {
			Console.WriteLine("SendPassword in");
			try {
				this.BeginSend((string) o);
			} catch (Exception e) {
				Common.Logger.WriteError("Unexpected error in timer callback method", e);
			}
			Console.WriteLine("SendPassword out");
		}

		#region Receieve

		private void BeginReceive() {
			byte[] buffer = this.receivingBuffer.bytes;

			this.socket.BeginReceive(buffer, 0,
				buffer.Length, SocketFlags.None, this.beginReceiveCallback, null);
		}

		private void BeginReceieveCallback(IAsyncResult asyncResult) {
			try {
				if ((this.socket != null) && (this.socket.Handle != IntPtr.Zero)) {
					int length = this.socket.EndReceive(asyncResult);

					if (length > 0) {
						//we have new data
						this.ProcessReceievedData(length);
					} else {
						this.Close("Connection lost");
					}

					if (this.IsConnected) {
						this.BeginReceive();
					}
				}
			} catch (Exception e) {
				Common.Logger.WriteDebug(e);
				this.Close(e.Message);
			}
		}

		static Regex loggedinRE = new Regex(@"(Login '(?<username>.+?)')|(?<badPass>Bad password for this account\.)|(?<inuse>Account already in use\.)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void ProcessReceievedData(int length) {
			StreamReader reader = new StreamReader(new MemoryStream(this.receivingBuffer.bytes, 0, length), Encoding.Default);

			this.receivingMode(reader);
		}

		private void ModeNotLoggedIn(TextReader reader) {
			string line;
			while ((line = reader.ReadLine()) != null) {
				Match m = loggedinRE.Match(line);
				if (m.Success) {
					if (m.Groups["badPass"].Value.Length > 0) {
						this.state.On_LoginSequenceFinished(LoginResult.BadPassword);
					} else if (m.Groups["inuse"].Value.Length > 0) {
						this.state.On_LoginSequenceFinished(LoginResult.AccountInUse);
					} else {
						if (m.Groups["username"].Value.Equals(((SphereServerSetup) this.state.Setup).AdminAccount,
							StringComparison.OrdinalIgnoreCase)) {

							this.receivingMode = this.loggedInMode;
							this.receivingMode(reader); //continue if there's anything

							this.state.On_LoginSequenceFinished(LoginResult.Success);
						} else {
							this.Close("unexpected username in login sequence, wtf?");
						}
					}
				}
			}
		}

		static Regex timeStampedLineRE = new Regex(
			@"^(?<time>[\d][\d]:[\d][\d]).*$",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void ModeLoggedIn(TextReader reader) {
			if (ConsoleServer.ConsoleServer.AllConsolesCount > 0) { //no consoles = no point processing any text
				string line;
				while ((line = reader.ReadLine()) != null) {
					this.state.On_ReceievedLine(line);
				}
			}
		}

		private void ModeCommandReply(TextReader reader) {
			if (this.commandResponders.Count > 0) {
				List<string> list = new List<string>();
				string line;
				while ((line = reader.ReadLine()) != null) { //timestamped lines are not part of the command response
					line = line.Trim();
					if (!string.IsNullOrEmpty(line)) {
						if (timeStampedLineRE.IsMatch(line)) {
							this.state.On_ReceievedLine(line);
						} else {
							list.Add(line);
						}
					}
				}
				if (list.Count > 0) {
					this.commandResponders.Dequeue().OnRespond(list);
				}
			} else {
				this.receivingMode = this.loggedInMode;
				this.receivingMode(reader);
			}
		}

		private void ModeIgnore(TextReader reader) {
		}

		#endregion Receieve

		public void SendCommand(string command, CommandResponse responder) {
			this.BeginSend(command);
			this.commandResponders.Enqueue(responder);
			this.receivingMode = this.commandReplyMode;
		}

		#region Send
		public void BeginSend(string message) {
			try {
				if (this.IsConnected) {
					Buffer buffer = Pool<Buffer>.Acquire();

					message = message + "\n";
					int len = Encoding.Default.GetBytes(message, 0, message.Length, buffer.bytes, 0);

					SocketError err;
					this.socket.BeginSend(buffer.bytes, 0, len, SocketFlags.None, out err, this.beginSendCallback, buffer);
					if (err != SocketError.Success) {
						this.Close(err.ToString());
					}
				}
			} catch (Exception e) {
				this.Close("Exception while BeginSend: " + e.Message);
				Common.Logger.WriteDebug(e);
			}
		}

		private void BeginSendCallback(IAsyncResult asyncResult) {
			Buffer toDispose = (Buffer) asyncResult.AsyncState;

			try {
				SocketError err;
				this.socket.EndSend(asyncResult, out err);

				if (err != SocketError.Success) {
					this.Close(err.ToString());
				}
			} catch (Exception e) {
				this.Close("Exception while BeginSendCallback: " + e.Message);
				Common.Logger.WriteDebug(e);
			} finally {
				toDispose.Dispose();
			}
		}
		#endregion Send

		#region Close
		public sealed override void Dispose() {
			this.Close("Dispose() called");
		}

		protected override void On_DisposeManagedResources() {
			this.receivingBuffer.Dispose();

			base.On_DisposeManagedResources();
		}

		protected override void On_DisposeUnmanagedResources() {
			if (this.state != null) {
				this.state.On_Close(this.closingReason);
			}
			this.receivingMode = this.ignoreMode;

			try {
				this.socket.Shutdown(SocketShutdown.Both);
			} catch {
			}
			try {
				this.socket.Close();
			} catch {
			}

			base.On_DisposeUnmanagedResources();
		}

		public void Close(string reason) {
			this.receivingMode = this.ignoreMode;
			if (this.closingReason == null) {
				lock (this) {
					if (this.closingReason == null) {
						this.closingReason = reason;
					}
					base.Dispose();
				}
			}
		}

		#endregion Close
	}

	public abstract class CommandResponse {

		internal abstract void OnRespond(IList<string> lines);
	}

	public delegate void ParserCallback<T>(IList<string> lines, T state);

	public class CallbackCommandResponse<T> : CommandResponse {
		ParserCallback<T> callback;
		T state;

		public CallbackCommandResponse(ParserCallback<T> callback, T state) {
			this.callback = callback;
			this.state = state;
		}

		internal override void OnRespond(IList<string> lines) {
			this.callback(lines, this.state);
		}
	}

	public enum LoginResult {
		Success,
		BadPassword,
		AccountInUse
	}
}
