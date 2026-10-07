using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchyUtils;
using Raven.Client.Document;
using Raven.Client.Embedded;
using SaveCruncher.Properties;

namespace SaveCruncher {
	public class SaveEntry {
		public string Id { get; set; }
		public string SectionType { get; set; }
		public string SectionName { get; set; }
		public Dictionary<string, string> Data = new Dictionary<string, string>();
	}
}
