namespace WOLF3D.WOLF3DGame.Setup
{
    public class Shareware
    {
        public static void Main(params string[] args)
        {
            Godot.Directory res = new Godot.Directory();
            res.Open("res://");

            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
                args = new string[] { System.IO.Directory.GetCurrentDirectory() };
            else
            {
                System.IO.Directory.CreateDirectory(args[0]);
                Godot.GD.Print("Created directory \"" + args[0] + "\"");
            }

            foreach (string file in ListFiles(null, "*.xml"))
            {
                string xml = System.IO.Path.Combine(
                    System.IO.Directory.CreateDirectory(
                        System.IO.Path.Combine(
                            args[0],
                            System.IO.Path.GetFileNameWithoutExtension(file)
                            )
                        ).FullName,
                    "game.xml"
                    );
                res.Copy("res://" + file, xml);
                Godot.GD.Print("Copied \"" + xml + "\"");
            }

            if (!System.IO.File.Exists(System.IO.Path.Combine(args[0], "WL1", "WOLF3D.EXE")))
            {
                // I'm including a complete and unmodified copy of Wolfenstein 3-D Shareware v1.4 retrieved from https://archive.org/download/Wolfenstein3d/Wolfenstein3dV14sw.ZIP in this game's resources which is used not only to play the shareware levels but also to render the game selection menu.
                // I would very much prefer to use the official URL ftp://ftp.3drealms.com/share/1wolf14.zip
                // However, that packs the shareware episode inside it's original installer, and extracting files from that is a pain.
                // To avoid that, I'll probably just use a zip of a fully installed shareware version instead.
                // In case I ever want to revisit the issue of extracting from the shareware installer, I found some C code to extract the shareware files here: https://github.com/rpmfusion/wolf3d-shareware
                // That code seems to depend on this library here: https://github.com/twogood/dynamite
                res.Copy("res://Wolfenstein3dV14sw.ZIP", System.IO.Path.Combine(args[0], "WL1", "Wolfenstein3dV14sw.ZIP"));
                System.IO.Compression.ZipFile.ExtractToDirectory(System.IO.Path.Combine(args[0], "WL1", "Wolfenstein3dV14sw.ZIP"), args[0]);
                System.IO.File.Delete(System.IO.Path.Combine(args[0], "WL1", "Wolfenstein3dV14sw.ZIP"));
            }
        }

        public static System.Collections.Generic.IEnumerable<string> ListFiles(string path = null, string filter = "*.*")
        {
            filter = WildCardToRegular(filter);
            Godot.Directory dir = new Godot.Directory();
            dir.Open(path ?? "res://");
            dir.ListDirBegin();
            while (dir.GetNext() is string file && !string.IsNullOrWhiteSpace(file))
                if (file[0] != '.' && System.Text.RegularExpressions.Regex.IsMatch(file, filter))
                    yield return file;
            dir.ListDirEnd();
        }

        public static string WildCardToRegular(string value) => "^" + System.Text.RegularExpressions.Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
    }
}
