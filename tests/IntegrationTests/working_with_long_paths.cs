namespace IntegrationTests
{
    using System.IO;
    using System.Text;
    using Minimal;
    using NUnit.Framework;

    [TestFixture]
    public class working_with_long_paths 
    {
        const string TempRoot = @"W:\Temp";

        [TestFixtureSetUp]
        public static void setup() {
            Assert.IsTrue(Directory.Exists(TempRoot), "Temp directory is not available. Please update tests with a usable temp directory");
        }

        [Test]
        public static void directory_paths_with_more_than_255_characters_are_supported() {

            var sb = new StringBuilder(TempRoot + "\\QIO");
            for (int i = 0; i < 35; i++)
            {
                sb.Append("\\Deeper");
                sb.Append(i);
            }

            var deepPath = new PathInfo(sb.ToString());
            Assert.That(deepPath.FullName.Length, Is.GreaterThan(255));
            NativeIO.CreateDirectory(deepPath, recursive: true);

            Assert.IsTrue(NativeIO.Exists(deepPath), "New directory was not created or could not be found");
            NativeIO.DeleteDirectory(new DirectoryDetail(TempRoot + "\\QIO"), recursive: true);
        }

        [Test]
        public static void files_can_be_created_and_written_and_read_and_copied_in_very_long_paths()
        {
            // Create a >255 length path
            const string path = TempRoot + "\\QIO\\Pseudopseudohypoparathyroidism\\Pneumonoultramicroscopicsilicovolcanoconiosis\\Floccinaucinihilipilification\\Antidisestablishmentarianism\\Honorificabilitudinitatibus\\Donau­dampf­schiffahrts­elektrizitäten­haupt­betriebs­werk­bau­unter­beamten­gesellschaft";
            NativeIO.CreateDirectory(new PathInfo(path), recursive: true);

            var sampleData = new byte[]{1,2,3,4,5,6,7,8};
            var srcFile = new PathInfo(path + "\\example.file.txt");
            var dstFile = new PathInfo(path + "\\example.copy.txt");

            // write a file
            using (var fs = NativeIO.OpenFileStream(srcFile, FileAccess.Write, FileMode.Create, FileShare.None)) {
                fs.Write(sampleData, 4, 4);
                fs.Write(sampleData, 0, 4);
                fs.Flush();
            }

            // copy the file elsewhere
            Assert.True(NativeIO.Exists(srcFile), "Source file can't be found (didn't write correctly?)");
            Assert.True(NativeIO.CopyFile(srcFile, dstFile), "Failed to copy file");
            Assert.True(NativeIO.Exists(dstFile), "Target file can't be found");

            // Check the contents
            using (var fs = NativeIO.OpenFileStream(srcFile, FileAccess.Read))
            {
                var buf = new byte[8];
                var length = fs.Read(buf, 0, 8);
                Assert.That(length, Is.EqualTo(8));
                Assert.That(buf, Is.EquivalentTo(new byte[] { 5, 6, 7, 8, 1, 2, 3, 4 }));
            }

            // cleanup
            NativeIO.DeleteDirectory(new DirectoryDetail(TempRoot + "\\QIO"), recursive: true);
        }
    }
}
