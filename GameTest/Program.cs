using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;
/*

  mana: 0
  mana_red: 0
  mana_blue: 0
  mana_green: 0
  mana_black: 0
  mana_white: 0
*/
class Core {
    public class Mana {
        private int overall;
        private int red;
        private int blue;
        private int green;
        private int black;
        private int white;
        private int eny;

        public int Overall { get => overall; set => overall = value; }
        public int Red { get => red; set => red = value; }
        public int Blue { get => blue; set => blue = value; }
        public int Green { get => green; set => green = value; }
        public int Black { get => black; set => black = value; }
        public int White { get => white; set => white = value; }
        public int Eny { get => eny; set => eny = value; }

        public Mana(int overall = 0, int red = 0, int blue = 0, int green = 0, int black = 0, int white = 0, int eny = 0) {
            this.Overall = overall;
            this.Red = red;
            this.Blue = blue;
            this.Green = green;
            this.Black = black;
            this.White = white;
            this.Eny = eny;
        }

    }
    public struct YAMLFile {
        private string path;

        private string id;
        private string content;

        public string Id { get => id; set => id = value; }
        public string Path { get => path; set => path = value; }
        public string Content { get => content; set => content = value; }

        public YAMLFile(string file, string id, string content) {
            this.Path = file;
            this.Content = content;
            this.Id = id;
        }
    }

    const string YAMLS_PATH = "yamls";

    public static object DoesPropertyExist(dynamic settings, string name) {
        if (settings is ExpandoObject) {
            return ((IDictionary<string, object>)settings).ContainsKey(name);
        }

        return settings.GetType().GetProperty(name) != null;
    }
    public static object GetPropertyValue(dynamic settings, string name) {
        if (settings is ExpandoObject) {
            if (((IDictionary<string, object>)settings).ContainsKey(name)) {
                return ((IDictionary<string, object>)settings)[name];
            }

            return "ZERO";
        }

        return settings.GetType().GetProperty(name) != null;
    }

    private static Mana parseMana(string the_value) {
        Mana mana_output = new();

        for(int i = 0; i < the_value.Length; i++) {
            string number = string.Empty;

            switch(the_value[i]) {
                case 'W':
                    mana_output.White++;
                    continue;
                case 'G':
                    mana_output.Green++;
                    continue;
                case 'B':
                    mana_output.Black++;
                    continue;
                case 'R':
                    mana_output.Red++;
                    continue;
                case 'X':
                    mana_output.Eny++;
                    continue;
                case 'U':
                    mana_output.Blue++;
                    continue;
            }
            while(char.IsDigit(the_value[i])) {
                number += the_value[i];
                i++;
            }
            if (number != string.Empty) {
                mana_output.Overall = Convert.ToInt32(number);
            }
        }

        return mana_output;
    }

    private static string parseTeam(char color) {
        return color switch {
            'R' => "red",
            'U' => "blue",
            'G' => "green",
            'W' => "white",
            'B' => "black",
            _ => throw new Exception("Unexpected value")
        };
    }

    private static YAMLFile GenerateYaml(string path, dynamic obj, string image_guid) {
        Mana mana = new();

        obj.team = "";

        int idenitities = 0;
        foreach(var ci in obj.color_identity) {
            obj.team += parseTeam(ci[0]);
            idenitities++;
        }
        if(idenitities == 0) {
            obj.team = "neutral";
        }

        if(DoesPropertyExist(obj, "mana_cost")) {
            mana = parseMana(obj.mana_cost);
        }

        obj.typetext = $"";
        obj.attack   = 0;
        obj.hp       = 0;
        obj.text     = $"";

        if(DoesPropertyExist(obj, "type_line")) {
            obj.typetext   = obj.type_line;
        }
        if(DoesPropertyExist(obj, "power")) {
            obj.attack     = obj.power;
        }
        if(DoesPropertyExist(obj, "toughness")) {
            obj.hp         = obj.toughness;
        }
        if(DoesPropertyExist(obj, "oracle_text")) {
            obj.text   = $"{obj.oracle_text}";
        }
        obj.title      = obj.name;
        obj.name_id    = obj.name;
        obj.mana       = mana.Overall;
        obj.mana_red   = mana.Red;
        obj.mana_white = mana.White;
        obj.mana_black = mana.Black;
        obj.mana_green = mana.Green;
        obj.mana_blue  = mana.Blue;
        obj.mana_eny   = mana.Eny;
        obj.deckbuilding = 1;
        obj.cost = 100;

        var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithDefaultScalarStyle(YamlDotNet.Core.ScalarStyle.DoubleQuoted).Build();
        string serialized = serializer.Serialize(obj);

        StringBuilder final = new(); 
        
        final.Append($@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 843a21f7f6f205741a0a7341aec8e84d, type: 3}}
  m_Name: " + obj.id + $@" 
  m_EditorClassIdentifier: 
  art_full: {{fileID: 21300000, guid: {image_guid}, type: 3}}
  art_board: {{fileID: 21300000, guid: {image_guid}, type: 3}}
");

        foreach(var strs in serialized.Split('\n')) {
            final.AppendLine($"  {strs}");
        }

        return new(path, obj.id + ".asset", final.ToString());
    }

    private static YAMLFile GenerateImage(string path, string id, string guid) {
        string content = @"fileFormatVersion: 2
guid: " + guid + @" 
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 11
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant:

";

        return new YAMLFile(path, id + ".jpg.meta", content);
    }

    private static void PutToQueue(YAMLFile yaml) {
        writing_queue.Enqueue(yaml);
    }
    private static void WriteToFile(YAMLFile yaml) {
        if(!Directory.Exists(yaml.Path)) {
            Directory.CreateDirectory(yaml.Path);
        }
        File.WriteAllText($"{yaml.Path}/{yaml.Id}", yaml.Content);
        saved_count++;
    }
    private static async void ExportYamls(Queue<dynamic> obj) {
        Stopwatch sw = new();
        sw.Start();
        if(!Directory.Exists(final_path)) {
            Directory.CreateDirectory(final_path);
        }

        while(obj.Count > 0) {
            dynamic dequeud = obj.Dequeue();
            string full_path = $"{final_path}/{((string)dequeud.set_name).Replace(' ', '_')}";
            PutToQueue(await GenerateImage(full_path, dequeud.id, dequeud.id));
//            PutToQueue(await GenerateYaml(full_path, dequeud));
        }

        sw.Stop();

        average_writing_time = (average_writing_time + sw.Elapsed.TotalSeconds) / 2;

        obj.Clear();
    }

    private static ulong saved_count = 0;
    private static double average_writing_time = 0;
    private static string final_path = YAMLS_PATH + "/" + DateTime.Now.ToString("HH:mm:dd:MMM:yyyy");
    private static List<Task> tasks = new List<Task>();
    private static ConcurrentQueue<YAMLFile> writing_queue = new ConcurrentQueue<YAMLFile>();

    private static Queue<dynamic> to_queue = new Queue<dynamic>();
    private static TaskFactory taskFactory = new TaskFactory();
    public static void Main(string[] args) {
        string path = "/Users/ivanbadikov/Downloads/all-cards-20240206101330.json";
        StringBuilder builder = new()
        {
            Capacity = 4096
        };

        List<Task> dequeued_tasks = new List<Task>();
        Queue<dynamic> ob = new Queue<dynamic>();

        bool over = false;
        double passtime = 0;
        Stopwatch pt = new(), tq = new(), tw = new(), overall_time = new(); 
        overall_time.Start();

        new Task( async () => {
            ulong prev = 0;
            while(!over) {
                Console.Clear();
                Console.WriteLine(
$@"Already saved: {saved_count.ToString().PadLeft(5, '0')}
Speed {(saved_count - prev) / 0.05} files/s 
Tasks {tasks.Count}
Dequeued Tasks {dequeued_tasks.Count}
Writing Queue {writing_queue.Count}
Pass time {pt.Elapsed.TotalSeconds}s
Queueing time {tq.Elapsed.TotalSeconds}s
Writing time {tw.Elapsed.TotalSeconds}s
Overall time {overall_time.Elapsed.ToString("mm\\:ss")}"
);
                prev = saved_count;
                await Task.Delay(50);
            }
        }).Start();

        new Task( async () => {
            tw.Start();
            while(!over) {
                tw.Restart();
                for(int i = 0; i < tasks.Count; i = i > 0 ? i : 0) {
                    if(tasks[i].IsCompleted) {
                        tasks.RemoveAt(i--);
                        continue;
                    }
                    i++;
                }
                if(writing_queue.Count < 1) {  
                    await Task.Delay(50);
                    continue; 
                }
                if(tasks.Count >= 6) {
                    continue;
                }
                Queue<YAMLFile> localQueue = new Queue<YAMLFile>();
                while(writing_queue.Count > 0) {
                    if(writing_queue.TryDequeue(out var result)) {
                        localQueue.Enqueue(result);
                    }
                }
                tasks.Add(new Task(() =>  { 
                    while(localQueue.Count > 0) {
                        WriteToFile(localQueue.Dequeue());
                    }
                }));
                tasks.Last().Start();
            }
        }).Start();


        using(StreamReader sr = new(path)) {
            string line = string.Empty;
            int braces = 0;
            ulong iters = 0;

            while((line = sr.ReadLine()) is not null) {
                bool eng = false;
                pt.Restart();

                    check_again: 
                    while(ob.Count > 0) {
                        dynamic dequeued = ob.Dequeue();
                        string full_path = $"{final_path}/{((string)dequeued.set_name).Replace(' ', '_')}";

                        while (dequeued_tasks.Count > 6) {
                            for(int i = 0; i < dequeued_tasks.Count; i = i > 0 ? i : 0) {
                                if(dequeued_tasks[i].IsCompleted) {
                                    dequeued_tasks.RemoveAt(i--);
                                    continue;
                                }
                                i++;
                            }
                        }
                        dequeued_tasks.Add(new Task(() => { 
                            string image_guid = ((string)dequeued.id).Replace("-", string.Empty);
                            PutToQueue(GenerateImage(full_path, dequeued.id, image_guid));
                            PutToQueue(GenerateYaml(full_path, dequeued, image_guid)); 
                        }));
                        dequeued_tasks.Last().Start();
                    }

                    ob.Clear();

                cont:

                for(int i = 0; i < line.Length; i++){
                    if(line[i] == '"' && i + 2 < line.Length) {
                        if(line[i+1] == 'e') {
                            if(line[i+2] == 'n') {
                                eng = true;
                            }
                        }
                    }
                    
                    if(line[i] == '{') {
                        braces++;
                    }
                    if(braces == 0) {
                        continue;
                    }
                    if(line[i] == '}') {
                        braces--;

                        if(braces == 0) {
                            builder.Append(line[i]);

                            if(eng) {
                                var expandoObjectConverter = new ExpandoObjectConverter();
                                dynamic converted = JsonConvert.DeserializeObject<ExpandoObject>(builder.ToString(), expandoObjectConverter);

                                ob.Enqueue(
                                    converted
                                );
                            }

                            builder.Clear();
                            continue;
                        }
                    }

                    builder.Append(
                        line[i] == '*' ? "STAR" : line[i]
                    );

                    if(i == line.Length - 1 && braces > 0) {
                        line = sr.ReadLine() ?? string.Empty;
                    }
                }
            }
            while(to_queue.Count > 0) {
            }

            for(int i = 0; i < dequeued_tasks.Count; i++) {
                if(dequeued_tasks[i].IsCompleted) {
                    dequeued_tasks.RemoveAt(i--);
                    continue;
                }
                if(tasks.Count > 0) {
                    Thread.Sleep(2000);
                    i = -1;
                }
            }
            for(int i = 0; i < tasks.Count; i++) {
                if(tasks[i].IsCompleted) {
                    tasks.RemoveAt(i--);
                    continue;
                }
                if(tasks.Count > 0) {
                    Thread.Sleep(2000);
                    i = -1;
                }
            }
            over = true;
            Thread.Sleep(1000);
            Console.WriteLine("Bye!");
        }
    }
}