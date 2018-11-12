using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ObjectBuilderCLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Build(args[0], args[1], args[2], args[3]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("Args are as follows:");
                Console.WriteLine(
                    "<executable> <Path to original JSON> <path to new images & offset files> <where to the final object> <author of the new object>");
            }
        }

        private static void Build(string json, string objectdata, string savefile, string author)
        {
            //This goes through the process of building our new object.
            var BaseJSON = LoadJSON(json);
            var SpritesAndOffsets = BuildObjectData(objectdata);
            BuildObject(BaseJSON, json, SpritesAndOffsets, savefile, author);
        }


        private static Dictionary<string, string> BuildObjectData(string folder)
        {
            var ImageOffset = new Dictionary<string, string>();
            foreach (var file in Directory.GetFiles(folder))
                if (file.ToUpper().EndsWith(".PNG"))
                {
                    var offsetFile = Path.ChangeExtension(file, "txt");
                    ImageOffset.Add(file, File.ReadAllText(offsetFile));
                }

            //This builds a Dictionary of the Sprites and their offsets.
            return ImageOffset;
        }

        private static JObject LoadJSON(string Path)
        {
            return JObject.Parse(File.ReadAllText(Path));
            //Gets us our JSON - I chose this method because we don't know the format of the JSON reliably.
        }

        private static void BuildObject(JObject json, string jsonpath, Dictionary<string, string> SpritesAndOffsets,
            string SaveLocation, string Authors)
        {
            //Our Goals here are as follows:
            //Mutate the JSON to work with our new object
            //Move Files into appropriate Locations
            //Generate the Object file
            //Move it to the designated save location.
            json.Remove("sourceGame");
            json["images"] = JToken.FromObject(convertFromDictionary(SpritesAndOffsets));
            //Removed sourcegame property - there isn't one.
            //Replaced the images list
            json["authors"].Replace(JToken.FromObject(Authors));

            var rand = new Random();
            var tempDir = Path.GetTempPath() + rand.Next(0, 5000);
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(tempDir + "/images");

            File.WriteAllText(tempDir + "/" + Path.GetFileName(jsonpath),
                JsonConvert.SerializeObject(json, Formatting.Indented));
            //^Creates our JSON file.
            foreach (var image in SpritesAndOffsets)
                File.Copy(image.Key, tempDir + "/images/" + Path.GetFileName(image.Key));
            ZipFile.CreateFromDirectory(tempDir, SaveLocation);
            Console.WriteLine("Process complete!");
        }

        private static List<ImagesObject> convertFromDictionary(Dictionary<string, string> startingData)
        {
            var P = new List<ImagesObject>();
            foreach (var item in startingData)
            {
                var tmp = new ImagesObject();
                tmp.path = "/images/" + Path.GetFileName(item.Key);
                var coords = item.Value.Split(' ');
                tmp.x = coords[0];
                tmp.y = coords[1];
                P.Add(tmp);
            }

            return P; //This converts our list of images and offsets into a list of ImageObjects.
        }

        public class ImagesObject
        {
            public string format = "raw";
            public string path;
            public string x;
            public string y;
        }
    }
}