using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using CrunchyUtils;
using Microsoft.Win32;
using SaveCruncher.Properties;

namespace SaveCruncher {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		static readonly string dataDirectory = Settings.Default.DataDir;

		public MainWindow() {
			InitializeComponent();
		}


		private void Window_Activated_1(object sender, EventArgs e) {
			if (Directory.Exists(dataDirectory)) {

				var path = Path.Combine(dataDirectory, "sphereworld.scp");
				if (File.Exists(path)) {
					this.tbWorldSavePath.Text = path;
				} else {
					this.tbWorldSavePath.Text = (from full in Directory.GetFiles(dataDirectory)
												 let file = Path.GetFileName(full)
												 where file.StartsWith("sphereb") && file.EndsWith("w.scp")
												 select full).FirstOrDefault();
				}

				path = Path.Combine(dataDirectory, "spherechars.scp");
				if (File.Exists(path)) {
					this.tbCharsSavePath.Text = path;
				} else {
					this.tbCharsSavePath.Text = (from full in Directory.GetFiles(dataDirectory)
												 let file = Path.GetFileName(full)
												 where file.StartsWith("sphereb") && file.EndsWith("c.scp")
												 select full).FirstOrDefault();
				}
			}
		}

		private void bWorldSavePathSelectFile_Click(object sender, RoutedEventArgs e) {
			GetPathFromOpenFileDialog(path => this.tbWorldSavePath.Text = path);
		}

		private void bCharsSavePathSelectFile_Click(object sender, RoutedEventArgs e) {
			GetPathFromOpenFileDialog(path => this.tbCharsSavePath.Text = path);
		}

		private void GetPathFromOpenFileDialog(Action<string> setter) {
			var dlg = new OpenFileDialog();
			dlg.DefaultExt = ".scp";
			if (Directory.Exists(dataDirectory)) {
				dlg.InitialDirectory = dataDirectory;
			}
			dlg.Filter = "sphere script/save file (.scp)|*.scp";

			var result = dlg.ShowDialog();

			if (result.HasValue && result.Value) {
				setter(dlg.FileName);
			}
		}

		private async void bLoadWorldSave_Click(object sender, RoutedEventArgs e) {
			string worldSavePath = this.tbWorldSavePath.Text;

			await ParseAndInsert(worldSavePath);
		}

		private async void bLoadCharsSave_Click(object sender, RoutedEventArgs e) {
			string charsSavePath = this.tbCharsSavePath.Text;
			await ParseAndInsert(charsSavePath);
		}

		private static async Task ParseAndInsert(string saveFilePath) {
			var db = await SaveDb.GetStoreAsync();


			await SaveParser.Parse(saveFilePath, entries => {
				using (var session = db.OpenSession()) {
					Logger.Write("Starting saving loadedentries to database (from '" + saveFilePath + "')");
					foreach (var e in entries) {
						session.Store(e);
					}					
					session.SaveChanges();
					Logger.Write("Finished saving loadedentries to database (from '" + saveFilePath + "')");
				}
			});
		}


		private async void bRunQuery_Click(object sender, RoutedEventArgs e) {

			var where = this.tbWhere.Text;
			var fieldNames = this.tbFieldNames.Text.Split(';').Select(s => s.Trim()).ToArray();

			this.dgResults.Columns.Clear();
			this.dgResults.AutoGenerateColumns = false;
			for (int i = 0; i < fieldNames.Length; i++) {
				var col = new DataGridTextColumn();
				col.Header = fieldNames[i];
				//Here i bind to the various indices.
				var binding = new Binding("[" + fieldNames[i] + "]");
				//var binding = new Binding(fieldNames[i]);
				col.Binding = binding;
				this.dgResults.Columns.Add(col);
			}

			var result = await SaveDb.Query(where, fieldNames);

			this.dgResults.ItemsSource = result;
		}
	}
}
