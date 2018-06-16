using OpenTK;
using SharpStrike;
using SharpStrike_Map_Editor.Properties;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace SharpStrike_Map_Editor
{
    public partial class Form1 : Form
    {
        private List<AxisAlignedBB> _boxes = new List<AxisAlignedBB>();

        private Bitmap _wall = Resources.wall;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            for (var index = 0; index < _boxes.Count; index++)
            {
                var box = _boxes[index];
                var size = box.size;

                e.Graphics.DrawImage(_wall, box.min.X, box.min.Y, size.X, size.Y);
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            _boxes.Add(new AxisAlignedBB(64).Offset(new Vector2(e.X - 32, e.Y - 32)));

            Save();

            Invalidate();
        }

        private void Save()
        {
            using (var fs = File.OpenWrite("map0.ssmap"))
            {
                var payload = new ByteBufferWriter(0);

                var floats = new List<float>();

                var count = 0;
                for (; count < _boxes.Count; count++)
                {
                    var box = _boxes[count];

                    floats.Add(box.min.X);
                    floats.Add(box.min.Y);
                    floats.Add(box.max.X);
                    floats.Add(box.max.Y);
                }

                payload.WriteInt32(count);

                foreach (var f in floats)
                {
                    payload.WriteFloat(f);
                }

                var data = payload.ToArray();

                fs.Write(data, 0, data.Length);
            }
        }
    }
}