using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using System.Drawing;

namespace table2image
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() != 2) { return; }

            if (System.IO.File.Exists(args[0]))
            {
                var tb = Table.LoadFile(args[0]);
                if (System.IO.Directory.Exists(args[1]))
                {
                    tb.SaveAsImage(System.IO.Path.Combine(args[1], System.IO.Path.GetFileNameWithoutExtension(args[0]) + CurrentSetting.DefaultExtension));
                }
                else
                {
                    tb.SaveAsImage(args[1]);
                }
            }
            else if(System.IO.Directory.Exists(args[0])){
                if (! System.IO.Directory.Exists(args[1]))
                {
                    System.IO.Directory.CreateDirectory(args[1]);
                }
                foreach (var file in System.IO.Directory.GetFiles(args[0]))
                {
                    if(! new string[] { ".htm",".html"}.Contains(System.IO.Path.GetExtension(file)))
                    {
                        continue;
                    }
                    try
                    {
                        var tb = Table.LoadFile(file);
                        tb.SaveAsImage(System.IO.Path.Combine(args[1], System.IO.Path.GetFileNameWithoutExtension(file) + CurrentSetting.DefaultExtension));
                    }
                    catch
                    {
                        Console.Error.WriteLine("Failed: "+file);
                    }
                }
            }
        }

        public static Setting CurrentSetting
        {
            get
            {
                if (_CurrentSetting != null) return _CurrentSetting;
                if (!System.IO.File.Exists(CurrentSettingPath))
                {
                    _CurrentSetting = new Setting();
                    Setting.Save(CurrentSettingPath, _CurrentSetting);
                    return _CurrentSetting;
                }
                return _CurrentSetting = Setting.Load(CurrentSettingPath);
            }
        }
        public static string CurrentSettingPath { get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "table2image.ini"); } }
        private static Setting _CurrentSetting = null;

        public class Setting
        {
            public Font CellFont = new Font("游ゴシック", 9);
            public Font CellHeadFont = new Font("游ゴシック", 9, FontStyle.Bold);
            public Font CaptionFont = new Font("游ゴシック", 9);

            public float SpacingX = 10;
            public float SpacingY = 10;

            public string DefaultExtension = ".jpeg";

            public string Encoding = "sjis";

            public class Font
            {
                public string Face= "游ゴシック";
                public float Size=9;
                public System.Drawing.FontStyle Style = 0;

                public Font()
                {
                }

                public Font(string Face,float Size, System.Drawing.FontStyle Style = 0)
                {
                    this.Face = Face;
                    this.Size = Size;
                    this.Style = Style;
                }

                public System.Drawing.Font GetFont()
                {
                    return new System.Drawing.Font(Face, Size, Style);
                }
            }

            public static Setting Load(string FileName)
            {
                if (!System.IO.File.Exists(FileName)) return new Setting();
                var des = new System.Xml.Serialization.XmlSerializer(typeof(Setting));
                using (var fs = new System.IO.StreamReader(FileName))
                {
                    try
                    {
                        return (Setting)des.Deserialize(fs);
                    }
                    catch
                    {
                    }
                }
                return new Setting();
            }

            public static void Save(string FileName, Setting arg)
            {
                var des = new System.Xml.Serialization.XmlSerializer(typeof(Setting));
                using (var fs = new System.IO.StreamWriter(FileName))
                {
                    try
                    {
                        des.Serialize(fs, arg);
                    }
                    catch
                    {
                    }
                }
            }
        }


        public class Table
        {
            public Cell Caption = new Cell();
            public Cell[,] Content;

            public static Table LoadFile(string arg)
            {
                string text;
                using (var sr = new System.IO.StreamReader(arg, Encoding.GetEncoding(CurrentSetting.Encoding)))
                {
                    text = sr.ReadToEnd();
                }
                var tb = new Table();
                tb.Load(text);
                return tb;
            }

            public void Load(string html)
            {
                var des = new System.Xml.Serialization.XmlSerializer(typeof(table));
                var htmlsr = new System.IO.StringReader(html);
                var tb = (table)des.Deserialize(htmlsr);

                this.Caption = new Cell() { Text = tb.caption };
                int columnCount = 0;
                foreach(var tr in tb.tr)
                {
                    columnCount = Math.Max(tr.Items.Count(), columnCount);
                }

                Content = new Cell[tb.tr.Count(), columnCount];
                int i = 0;
                foreach (var tr in tb.tr)
                {
                    int j = 0;
                    for(; j < tr.Items.Count(); j++)
                    {
                        Content[i, j] = new Cell() { Text = tr.Items[j], Head = tr.ItemsElementName[j] == ItemsChoiceType.th };
                    }
                    for (; j < columnCount; j++)
                    {
                        Content[i, j] = new Cell();
                    }
                    i++;
                }
            }

            public void SaveAsImage(string arg)
            {
                float spacingX = CurrentSetting.SpacingX;
                float spacingY = CurrentSetting.SpacingY;

                Bitmap testCanvas = new Bitmap(640, 480);
                Graphics testG = Graphics.FromImage(testCanvas);

                Font CellFont= CurrentSetting.CellFont.GetFont();
                Font CellHeadFont = CurrentSetting.CellHeadFont.GetFont();
                Font CaptionFont= CurrentSetting.CaptionFont.GetFont();

                var ColumnMaxSize = new float[Content.GetLength(1)];
                var RowMaxSize = new float[Content.GetLength(0)];
                for (int j = 0; j < Content.GetLength(1); j++)
                {
                    ColumnMaxSize[j] = 0;
                }

                float tableWidth = spacingX;
                float canvasHeight = spacingY;
                for (int i = 0; i < Content.GetLength(0); i++)
                {
                    RowMaxSize[i] = 0;
                    for (int j = 0; j < Content.GetLength(1); j++)
                    {
                        var size = testG.MeasureString(Content[i, j].Text, Content[i, j].Head ? CellHeadFont : CellFont);
                        Content[i, j].Width = size.Width;
                        Content[i, j].Height = size.Height;
                        RowMaxSize[i] = Math.Max(RowMaxSize[i], size.Height);
                        ColumnMaxSize[j] = Math.Max(ColumnMaxSize[j], size.Width);
                    }
                    canvasHeight += RowMaxSize[i] + spacingY;
                }

                for (int j = 0; j < Content.GetLength(1); j++)
                {
                    tableWidth += ColumnMaxSize[j] + spacingX;
                }

                int tableHeight = (int)(canvasHeight - spacingY / 2);

                int canvasWidth = (int)tableWidth;

                if (Caption.Text != "")
                {
                    var size = testG.MeasureString(Caption.Text, CaptionFont);
                    canvasHeight += size.Height + spacingY / 2;
                    canvasWidth = (int)Math.Max(tableWidth, size.Width);

                    Caption.Width = size.Width;
                    Caption.Height = size.Height;
                }
                canvasHeight -= spacingY / 2;

                Bitmap Canvas = new Bitmap((int)canvasWidth, (int)canvasHeight);
                Graphics g = Graphics.FromImage(Canvas);
                g.Clear(Color.White);
                float x = (canvasWidth - tableWidth) / 2 + spacingX / 2;
                float y = spacingY/2;

                g.DrawLine(Pens.Black, new Point((int)((canvasWidth - tableWidth) / 2 + spacingX / 2), (int)y), new Point((int)(canvasWidth / 2 + tableWidth / 2 - spacingX / 2.0), (int)y));
                g.DrawLine(Pens.Black, new Point((int)x, (int)spacingY/2), new Point((int)x, tableHeight));
                for (int j = 0; j < Content.GetLength(1); j++)
                {
                    x += ColumnMaxSize[j] + spacingX;
                    g.DrawLine(Pens.Black, new Point((int)x, (int)spacingY / 2), new Point((int)x, tableHeight));
                }
                x = (canvasWidth - tableWidth) / 2 + spacingX / 2;
                for (int i = 0; i < Content.GetLength(0); i++)
                {
                    for (int j = 0; j < Content.GetLength(1); j++)
                    {
                        var cell = Content[i, j];
                        g.DrawString(cell.Text, cell.Head ? CellHeadFont : CellFont, Brushes.Black, x + (int)(ColumnMaxSize[j] / 2.0 - cell.Width / 2.0 + spacingX / 2.0), y + (int)(RowMaxSize[i] / 2.0 - cell.Height / 2.0 + spacingY / 2.0));
                        x += ColumnMaxSize[j] + spacingX;
                    }
                    x = (canvasWidth - tableWidth) / 2 + spacingX / 2;
                    y += RowMaxSize[i] + spacingY;
                    g.DrawLine(Pens.Black, new Point((int)((canvasWidth - tableWidth) / 2 + spacingX / 2), (int)y), new Point((int)(canvasWidth / 2 + tableWidth / 2 - spacingX / 2.0), (int)y));
                }
                g.DrawString(Caption.Text, CaptionFont, Brushes.Black, (int)(canvasWidth / 2.0 - Caption.Width / 2.0), y + (int)(spacingY/2.0));

                Canvas.Save(arg);
            }

            public class Cell
            {
                public string Text = "";
                public bool Head = false;
                public float Width;
                public float Height;
            }
        }
    }
}
