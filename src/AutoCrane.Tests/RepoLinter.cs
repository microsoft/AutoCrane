using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoCrane.Tests
{
    [TestClass]
    public class RepoLinter
    {
        [TestMethod]
        public void TestLineFeeds()
        {
            var dir = Directory.GetCurrentDirectory();
            string? root = null;
            for (var i = 0; i < 20 && !string.IsNullOrEmpty(dir); i++)
            {
                var filesInDir = Directory.GetFiles(dir);
                if (filesInDir.Any(f => f.EndsWith(".editorconfig")))
                {
                    root = dir;
                    break;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            Assert.IsNotNull(root);
            if (root is not null)
            {
                var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
                var dirtyFiles = new List<string>();
                foreach (var file in csFiles)
                {
                    if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                    {
                        continue;
                    }

                    // \r character
                    if (File.ReadAllBytes(file).Any(b => b == 13))
                    {
                        dirtyFiles.Add(file);
                    }
                }

                Assert.IsFalse(dirtyFiles.Any(), $"Files containing return carriages: {string.Join('\n', dirtyFiles)}");
            }
        }

    }
}
