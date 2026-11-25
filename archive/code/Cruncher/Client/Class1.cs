
			
			//completeDate.ThatMatches().AndReturns();
			//hexNumber.ThatMatches(@"").AndReturns(f => int.Parse(f, NumberStyles.HexNumber));
			//decNumber.ThatMatches(@"[0-9]+").AndReturns(f => int.Parse(f));
			////anything.ThatMatches(".+").AndReturns(f => f);
			//alphanumeric.ThatMatches("[a-zA-Z0-9_]+").AndReturns(f => f);
			//eol.ThatMatches(Environment.NewLine);

			////var firstLine = config.Rule();
			////var unknownLogLine = config.Rule();
			//var anyLogLine = config.Rule();
			////var logFile = config.Rule();

			//var clientActionLine = config.Rule();
			//var loginLine = config.Rule();
			////var clientConnectedLine = config.Rule();
			//var clientDisconnectedLine = config.Rule();


			////firstLine.IsMadeUp.By(completeDate).As("LogStartTime").Followed.By(eol).WhenFound(f => f.LogStartTime);
			////unknownLogLine.IsMadeUp.By(hoursMinutes).As("LineTime").Followed.By(anything).As("Text").WhenFound(f => f.LineTime + ":::" + f.Text);
			//anyLogLine.IsMadeUp.By(hoursMinutes).As("LineTime").
			//	Followed.By(clientActionLine).As("LogLineObject")
			//	.WhenFound(f => {
			//		var l = (LogLine) f.LogLineObject;
			//		if (!l.isTimeExact) {
			//			l.time = f.LineTime;
			//		}
			//		return l;
			//	});

			////logFile.IsMadeUp.ByListOf(unknownLogLine);

			//clientActionLine.IsMadeUp.By(hexNumber).As("ClientId").Followed.By(":").Followed
			//	.By(loginLine).As("LogLineObject")
			//	//.Or.By(clientConnectedLine).As("LogLineObject")
			//	.Or.By(clientDisconnectedLine).ClientActionLineConfig()
			//	;

			//loginLine.IsMadeUp.By("Login '").Followed.By(alphanumeric).As("AccountName").Followed.By("'")
			//	.WhenFound(f => new ClientLoginLine { accountName = f.AccountName });

			//clientDisconnectedLine.IsMadeUp.By("Client disconnected [Total:").Followed.By(decNumber).As("TotalClients").Followed.By("]")
			//	.WhenFound(f => new ClientDisconnectedLine { totalClients = f.TotalClients });

			////clientConnectedLine.IsMadeUp.By("Client connected [Total:").Followed.By(alphanumeric).As("AccountName").Followed.By("'")
			////	.WhenFound(f => new ClientLoginLine { accountName = f.AccountName });

			//var parser = config.CreateParser();

			////foreach (var line in File.ReadLines("sphere.log")){
			////	var r = parser.Parse(line);
			////}

			////using (var logStream = File.OpenText("sphere.log")) {
			////	logStream.ReadLine();

			////	var r = parser.Parse(logStream);
			////}


			////var log = @"00:00:(funkce.scp,4999)'hwanin' commands uid=01121b4 (Endagor Algir) to 's(2012/04/01 00:00:00 made item uid=#04009d21d (vrhaci nuz))' OK";
