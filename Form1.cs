using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json.Serialization;

namespace FlowchartEditor
{
    public class Node
    {

        public int Id { get; set; }
        public string Text { get; set; }
        public Point Position { get; set; }

        [JsonIgnore]
        public Rectangle Bounds => new Rectangle(Position, new Size(100, 40));

        [JsonIgnore]
        public Point TopCenter => new Point(Position.X + 50, Position.Y);
        [JsonIgnore]
        public Point BottomCenter => new Point(Position.X + 50, Position.Y + 40);
        [JsonIgnore]
        public Point LeftCenter => new Point(Position.X, Position.Y + 20);
        [JsonIgnore]
        public Point RightCenter => new Point(Position.X + 100, Position.Y + 20);

        public override string ToString()
        {
            return $"{Id}: {Text}";
        }

    }

    public class Connection
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public string Label { get; set; }
        public override string ToString() =>
            $"{FromId} → {ToId}" + (string.IsNullOrEmpty(Label) ? "" : $" [{Label}]");

    }
    public class FlowchartData
    {
        public List<Node> Nodes { get; set; }
        public List<Connection> Connections { get; set; }
    }

    public partial class Form1 : Form
    {
        private List<Node> nodes = new List<Node>();
        private List<Connection> connections = new List<Connection>();
        private int nodeIdCounter = 1;

        // UI controls
        private TextBox txtNode, txtLabel;
        private Button btnAddNode, btnDeleteNode, btnConnect, btnDeleteConn;
        private Button btnSaveJson, btnLoadJson, btnExport;
        private ListBox lstNodes, lstConnections;
        private Label lblLabel;
        private Panel canvas;

        public Form1()
        {
            InitializeComponent();

            Text = "Flowchart Editor";
            Width = 1200;
            Height = 650;
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Node input
            txtNode = new TextBox { Location = new Point(10, 10), Width = 200 };
            btnAddNode = new Button { Text = "Add Node", Location = new Point(220, 10) };
            btnAddNode.Click += BtnAddNode_Click;

            btnDeleteNode = new Button { Text = "Delete Node", Location = new Point(320, 10) };
            btnDeleteNode.Click += BtnDeleteNode_Click;

            lstNodes = new ListBox
            {
                Location = new Point(10, 50),
                Size = new Size(300, 200),
                SelectionMode = SelectionMode.MultiExtended,
                FormattingEnabled = false  // ← ここ
            };


            // Connection controls
            lblLabel = new Label { Text = "Connection Label (opt)", Location = new Point(10, 260), AutoSize = true };
            txtLabel = new TextBox { Location = new Point(10, 280), Width = 150 };
            btnConnect = new Button { Text = "Connect", Location = new Point(170, 280) };
            btnConnect.Click += BtnConnect_Click;

            lstConnections = new ListBox
            {
                Location = new Point(10, 320),
                Size = new Size(300, 100),
                SelectionMode = SelectionMode.MultiExtended
            };

            btnDeleteConn = new Button { Text = "Delete Connection", Location = new Point(10, 430) };
            btnDeleteConn.Click += BtnDeleteConn_Click;

            // Save/Load
            btnSaveJson = new Button { Text = "Save JSON", Location = new Point(150, 430) };
            btnSaveJson.Click += BtnSaveJson_Click;
            btnLoadJson = new Button { Text = "Load JSON", Location = new Point(240, 430) };
            btnLoadJson.Click += BtnLoadJson_Click;

            // Export image
            btnExport = new Button { Text = "Export Image", Location = new Point(10, 470) };
            btnExport.Click += BtnExport_Click;

            // Canvas
            canvas = new Panel
            {
                Location = new Point(320, 10),
                Size = new Size(850, 600),
                BorderStyle = BorderStyle.FixedSingle
            };
            canvas.Paint += Canvas_Paint;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
//            canvas.MouseDoubleClick += Canvas_MouseDoubleClick;

            Controls.AddRange(new Control[]
            {
                txtNode, btnAddNode, btnDeleteNode, lstNodes,
                lblLabel, txtLabel, btnConnect, lstConnections,
                btnDeleteConn, btnSaveJson, btnLoadJson, btnExport,
                canvas
            });

            var btnAutoConnect = new Button { Text = "Auto‑Connect", Location = new Point(10, 510) };
            btnAutoConnect.Click += BtnAutoConnect_Click;
            Controls.Add(btnAutoConnect);

        }

        private void BtnAddNode_Click(object _, EventArgs e)
        {
            string text = txtNode.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var node = new Node
            {
                Id = nodeIdCounter++,
                Text = text,
                Position = new Point(50 + (nodes.Count % 5) * 120, 100 + (nodes.Count / 5) * 100)
            };
            nodes.Add(node);
            lstNodes.Items.Add(node);
            canvas.Invalidate();
        }

        private void BtnDeleteNode_Click(object _, EventArgs e)
        {
            var sel = lstNodes.SelectedItems.Cast<Node>().ToList();
            foreach (var n in sel)
            {
                nodes.Remove(n);
                lstNodes.Items.Remove(n);
                connections.RemoveAll(c => c.FromId == n.Id || c.ToId == n.Id);
            }
            UpdateConnectionList();
            canvas.Invalidate();
        }

        private void BtnConnect_Click(object _, EventArgs e)
        {
            if (lstNodes.SelectedItems.Count != 2) return;
            var from = (Node)lstNodes.SelectedItems[0];
            var to = (Node)lstNodes.SelectedItems[1];
            connections.Add(new Connection { FromId = from.Id, ToId = to.Id, Label = txtLabel.Text.Trim() });
            UpdateConnectionList();
            canvas.Invalidate();
        }

        private void BtnDeleteConn_Click(object _, EventArgs e)
        {
            var sel = lstConnections.SelectedItems.Cast<Connection>().ToList();
            foreach (var c in sel)
                connections.Remove(c);

            UpdateConnectionList();
            canvas.Invalidate();
        }

        private void BtnSaveJson_Click(object _, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "JSON file (*.json)|*.json|All files (*.*)|*.*";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var data = new FlowchartData { Nodes = nodes, Connections = connections };
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(data, opts));
                MessageBox.Show("保存しました: " + dlg.FileName);
            }
        }

        private void BtnLoadJson_Click(object _, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "JSON file (*.json)|*.json|All files (*.*)|*.*";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var data = JsonSerializer.Deserialize<FlowchartData>(File.ReadAllText(dlg.FileName));
                    nodes = data?.Nodes ?? new List<Node>();
                    connections = data?.Connections ?? new List<Connection>();
                    nodeIdCounter = nodes.Any() ? nodes.Max(n => n.Id) + 1 : 1;
                    lstNodes.Items.Clear();
                    foreach (var n in nodes) lstNodes.Items.Add(n);
                    UpdateConnectionList();
                    canvas.Invalidate();
                    MessageBox.Show("読み込み完了");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("読み込みエラー: " + ex.Message);
                }
            }
        }

        private void BtnExport_Click(object _, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";
                dlg.Title = "Export Flowchart Image";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                using (var bmp = new Bitmap(canvas.Width, canvas.Height))
                {
                    canvas.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    switch (Path.GetExtension(dlg.FileName).ToLower())
                    {
                        case ".jpg":
                        case ".jpeg":
                            bmp.Save(dlg.FileName, ImageFormat.Jpeg); break;
                        case ".bmp":
                            bmp.Save(dlg.FileName, ImageFormat.Bmp); break;
                        default:
                            bmp.Save(dlg.FileName, ImageFormat.Png); break;
                    }
                }
                MessageBox.Show("保存しました: " + dlg.FileName);
            }
        }

        /*        private void Canvas_MouseDoubleClick(object _, MouseEventArgs e)
                {
                    var node = nodes.FirstOrDefault(n => n.Bounds.Contains(e.Location));
                    if (node != null)
                    {
                        string input = Microsoft.VisualBasic.Interaction.InputBox("ノード名を入力", "Edit Node", node.Text);
                        if (!string.IsNullOrWhiteSpace(input))
                            node.Text = input;
                        canvas.Invalidate();
                    }
                }
        */

        private void BtnAutoConnect_Click(object _, EventArgs e)
        {
            // ID順にノードをソートして接続を作成
            var ordered = nodes.OrderBy(n => n.Id).ToList();
            connections.Clear();
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                var from = ordered[i];
                var to = ordered[i + 1];
                connections.Add(new Connection { FromId = from.Id, ToId = to.Id });
            }
            UpdateConnectionList();
            canvas.Invalidate();
        }

        private void UpdateConnectionList()
        {
            lstConnections.Items.Clear();
            foreach (var c in connections)
                lstConnections.Items.Add(c);
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // 接続（Connection）を順に描画
            foreach (var conn in connections)
            {
                var from = nodes.FirstOrDefault(n => n.Id == conn.FromId);
                var to = nodes.FirstOrDefault(n => n.Id == conn.ToId);
                if (from == null || to == null) continue;

                Point start, end;

                // 接続方向に応じた始点と終点を決定
                if (Math.Abs(from.Position.X - to.Position.X) < 50)
                {
                    start = from.BottomCenter;
                    end = to.TopCenter;
                }
                else if (from.Position.X < to.Position.X)
                {
                    start = from.RightCenter;
                    end = to.LeftCenter;
                }
                else
                {
                    start = from.LeftCenter;
                    end = to.RightCenter;
                }

                // 直角折れ線で接続を描画
                DrawRightAngleConnection(g, start, end);

                // ラベルがある場合は中央に描画
                if (!string.IsNullOrEmpty(conn.Label))
                {
                    int midX = (start.X + end.X) / 2;
                    int midY = (start.Y + end.Y) / 2;
                    g.DrawString(conn.Label, Font, Brushes.DarkRed, midX, midY);
                }
            }

            // ノード（長方形＋テキスト）を描画
            foreach (var node in nodes)
            {
                Rectangle rect = node.Bounds;
                g.FillRectangle(Brushes.LightBlue, rect);
                g.DrawRectangle(Pens.Black, rect);
                g.DrawString(node.Text, Font, Brushes.Black, rect);
            }
        }

        private void DrawRightAngleConnection(Graphics g, Point from, Point to)
        {
            List<Point> path = new List<Point>();

            if (Math.Abs(from.X - to.X) < 10)
            {
                int midY = (from.Y + to.Y) / 2;
                path.Add(from);
                path.Add(new Point(from.X, midY));
                path.Add(new Point(to.X, midY));
                path.Add(to);
            }
            else
            {
                int midX = (from.X + to.X) / 2;
                path.Add(from);
                path.Add(new Point(midX, from.Y));
                path.Add(new Point(midX, to.Y));
                path.Add(to);
            }

            g.DrawLines(Pens.Black, path.ToArray());
            DrawArrowHead(g, path[path.Count - 2], to);
        }

        private void DrawArrowHead(Graphics g, Point from, Point to)
        {
            var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);

            Point p1 = to;
            Point p2 = new Point((int)(to.X - 10 * cos + 5 * sin), (int)(to.Y - 10 * sin - 5 * cos));
            Point p3 = new Point((int)(to.X - 10 * cos - 5 * sin), (int)(to.Y - 10 * sin + 5 * cos));

            g.FillPolygon(Brushes.Black, new Point[] { p1, p2, p3 });
        }

        private Node draggingNode = null;
        private Point dragOffset;

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            foreach (var node in nodes)
            {
                if (node.Bounds.Contains(e.Location))
                {
                    draggingNode = node;
                    dragOffset = new Point(e.X - node.Position.X, e.Y - node.Position.Y);
                    break;
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggingNode != null && e.Button == MouseButtons.Left)
            {
                draggingNode.Position = new Point(e.X - dragOffset.X, e.Y - dragOffset.Y);
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            draggingNode = null;
        }
    }
}
