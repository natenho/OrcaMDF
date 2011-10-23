using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OrcaMDF.Core.Engine;

namespace OrcaMDF.Core.Tests.Features.MultiDataFile
{
	public class MultiFileTests : SqlServerSystemTest
	{
		[Test]
		public void RoundRobinHeapAllocation()
		{
			using (var db = new Database(DataFilePaths))
			{
				var scanner = new DataScanner(db);
				var rows = scanner.ScanTable("RoundRobinHeap").ToList();

				Assert.AreEqual(100, rows.Count);
			}
		}

		[Test]
		public void RoundRobinClusteredAllocation()
		{
			using (var db = new Database(DataFilePaths))
			{
				var scanner = new DataScanner(db);
				var rows = scanner.ScanTable("RoundRobinClustered").ToList();

				Assert.AreEqual(100, rows.Count);
			}
		}

		[Test]
		public void FGSpecificHeapAllocation()
		{
			using (var db = new Database(DataFilePaths))
			{
				var scanner = new DataScanner(db);
				var rows = scanner.ScanTable("FGSpecificHeap").ToList();

				Assert.AreEqual(100, rows.Count);
			}
		}

		[Test]
		public void FGSpecificClusteredAllocation()
		{
			using (var db = new Database(DataFilePaths))
			{
				var scanner = new DataScanner(db);
				var rows = scanner.ScanTable("FGSpecificClustered").ToList();

				Assert.AreEqual(100, rows.Count);
			}
		}

		protected override short GetNumberOfFiles()
		{
			return 3;
		}

		protected override void RunSetupQueries(SqlConnection conn)
		{
			// A normal heap that'll be round robin allocated among the data files.
			// As first 8 pages are stored in the IAM page header, and thus in the same
			// data file, we'll create 100 to hit multiple data files
			string query = "CREATE TABLE RoundRobinHeap (A int identity, B char(6000));";
			for (int i = 0; i < 100; i++)
				query += "INSERT INTO RoundRobinHeap DEFAULT VALUES;";
			RunQuery(query, conn);

			// Test the same with a clustered table
			query = "CREATE TABLE RoundRobinClustered (A int identity, B char(6000));";
			for (int i = 0; i < 100; i++)
				query += "INSERT INTO RoundRobinClustered DEFAULT VALUES;";
			RunQuery(query, conn);

			// Create a new filegroup, add a new data file and create a new heap on this FG
			RunQuery("ALTER DATABASE [" + DatabaseName + "] ADD FILEGROUP [SecondaryFilegroup]", conn);
			RunQuery("ALTER DATABASE [" + DatabaseName + "] ADD FILE ( NAME = N'SecondaryFGFile', FILENAME = N'" + Path.Combine(DataFileRootPath, DatabaseName + "_SecondFG.ndf") + "' , SIZE = 3072KB , FILEGROWTH = 1024KB ) TO FILEGROUP [SecondaryFilegroup]", conn);
			query = "CREATE TABLE FGSpecificHeap (A int identity, B char(6000));";
			for (int i = 0; i < 100; i++)
				query += "INSERT INTO FGSpecificHeap DEFAULT VALUES;";
			RunQuery(query, conn);

			// Test the same with a clustered table
			query = "CREATE TABLE FGSpecificClustered (A int identity, B char(6000));";
			for (int i = 0; i < 100; i++)
				query += "INSERT INTO FGSpecificClustered DEFAULT VALUES;";
			RunQuery(query, conn);
		}
	}
}