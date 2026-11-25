using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrunchyUtils;
using Raven.Client.Document;
using Raven.Client.Embedded;
using SaveCruncher.Properties;

namespace SaveCruncher {

	class SaveDb {



		public static Lazy<EmbeddableDocumentStore> Store = new Lazy<EmbeddableDocumentStore>(CreateStoreInstance, LazyThreadSafetyMode.ExecutionAndPublication);

		public static async Task<EmbeddableDocumentStore> GetStoreAsync() {
			var db = await Task.Run(() => Store.Value);

			return db;
		}

		private static EmbeddableDocumentStore CreateStoreInstance() {
			Logger.Write("Starting database initialisation");

			var dbDir = Path.Combine(Settings.Default.DataDir, Settings.Default.DatabaseSubDir);

			//we start anew every time, for now
			try {
				Directory.Delete(dbDir, true);
			} catch { }


			var db = new EmbeddableDocumentStore {
				DataDirectory = dbDir
			};
			db.Initialize();

			Logger.Write("Finished database initialisation");
			return db;
		}

		public static async Task<List<Dictionary<string, object>>> Query(string where, string[] fieldNames) {
		//public static async Task<List<SaveEntry>> Query(string where, string[] fieldNames) {
			Logger.Write("Starting query");

			var db = Store.Value;

			using (var session = db.OpenSession()) {

				//var q = session.Advanced.LuceneQuery<SaveEntry>().Where(where);

				var e2 = await Task.Run(() =>
					session.Advanced.LuceneQuery<SaveEntry>()
					.Where(where)
					.SelectFields<Dictionary<string, object>>(fieldNames)
					.ToList());

				//var e2 = session.Query<SaveEntry>().Where(e => e.Id == id).ToList();

				Logger.Write("Finished query");
				return e2;
			}

		}

	}
}
