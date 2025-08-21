// See https://aka.ms/new-console-template for more information
using Snapper.PMP;
using MareSynchronos.Export;
using Snapper;

Configuration configuration = new Configuration();
configuration.WorkingDirectory = ".";
MareCharaFileManager fileManager = new MareCharaFileManager(configuration);

Console.WriteLine("Enter path to MCDF");
string mcdf_path = Console.ReadLine();

fileManager.LoadMareCharaFile(mcdf_path);
string snapshot_path = fileManager.ExtractMareCharaFile();

PMPExportManager pmpManager = new PMPExportManager(configuration);
pmpManager.SnapshotToPMP(snapshot_path);
