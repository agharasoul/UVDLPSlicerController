﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Resources;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using UV_DLP_3D_Printer._3DEngine;
using UV_DLP_3D_Printer.Plugin;

namespace UV_DLP_3D_Printer.GUI.CustomGUI
{
    abstract class DecorItem
    {
        public abstract void Show(C2DGraphics g2d, int w, int h);
    }

    class DecorImage : DecorItem
    {
        public DecorImage(string name, string docking, int x, int y, Color col)
        {
            this.name = name;
            this.docking = docking;
            this.x = x;
            this.y = y;
            this.color = col;
        }
        string name;
        string docking; // tl = top left, rc = right center, nn = no dock, etc
        int x, y;       // gap to edge when docked, absolute if not
        Color color;

        public override void Show(C2DGraphics g2d, int w, int h)
        {
            int iw = 0, ih = 0;
            g2d.GetImageDim(name, ref iw, ref ih);
            int px = GuiConfig.GetPosition(0, w, iw, x, docking[1]);
            int py = GuiConfig.GetPosition(0, h, ih, y, docking[0]);
            g2d.SetColor(color);
            g2d.Image(name, px, py);
        }
    }

    class DecorBar : DecorItem
    {
        public DecorBar(string docking, int w, Color col) // solid bar
        {
            this.docking = docking;
            this.bw = w;
            coltl = col;
            coltr = col;
            colbl = col;
            colbr = col;
        }

        public DecorBar(string docking, int w, Color coltl, Color coltr, Color colbl, Color colbr) // gradient bar
        {
            this.docking = docking;
            this.bw = w;
            this.coltl = coltl;
            this.coltr = coltr;
            this.colbl = colbl;
            this.colbr = colbr;
        }

        string docking; // t = top, b = bottom, l = left, r = right, n = no dock (fullscreen)
        int bw;         // bar width
        Color coltl, coltr, colbl, colbr;
        public override void Show(C2DGraphics g2d, int w, int h)
        {
            int px, py, pw, ph;
            switch (docking)
            {
                case "t": px = 0; py = 0; pw = w; ph = bw; break;
                case "b": px = 0; py = h - bw; pw = w; ph = bw; break;
                case "l": px = 0; py = 0; pw = bw; ph = h; break;
                case "r": px = w - bw; py = 0; pw = bw; ph = h; break;
                default: px = 0; py = 0; pw = w; ph = h; break;
            }
            g2d.GradientRect(px, py, pw, ph, coltl, coltr, colbl, colbr);
        }
    }

    public class ControlStyle
    {
        public static Color NullColor = Color.FromArgb(1);

        public ControlStyle(Color forecol, Color backcol)
        {
            ForeColor = forecol;
            BackColor = backcol;
            FrameColor = NullColor;
            BackImage = null;
        }

        public ControlStyle()
        {
            ForeColor = NullColor;
            BackColor = NullColor;
            FrameColor = NullColor;
            BackImage = null;
        }

        public String Name;
        public Color ForeColor;
        public Color BackColor;
        public Color FrameColor;
        public String BackImage;
        public bool glMode;
        public virtual void SetDefault()
        {
            ForeColor = Color.White;
            BackColor = Color.Transparent;
            glMode = false;
        }
    }

    public class ButtonStyle : ControlStyle
    {
        public int SubImgCount;
        public Color PressedColor;
        public Color CheckedColor;
        public Color DisabledColor;
        public Color HoverColor;
        String mCheckedImage;
        C2DImage mCheckedImageCach;
        public override void SetDefault()
        {
            base.SetDefault();
            CheckedColor = Color.Orange;
            PressedColor = Color.White;
            HoverColor = Color.White;
            DisabledColor = Color.FromArgb(60, 255, 255, 255);
            SubImgCount = 4;
            glMode = false;
        }

        public String CheckedImage
        {
            get { return mCheckedImage; }
            set
            {
                mCheckedImage = value;
                mCheckedImageCach = null;
            }
        }

        public C2DImage CheckedImageCach
        {
            get
            {
                if (mCheckedImageCach == null)
                    mCheckedImageCach = UVDLPApp.Instance().m_2d_graphics.GetImage(mCheckedImage);
                return mCheckedImageCach;
            }
        }
    }

    public class GuiConfig
    {
        public enum EntityType { Buttons, Panels, Decals }

        Dictionary<String, ctlUserPanel> Controls;
        Dictionary<String, ctlImageButton> Buttons;
        Dictionary<String, ControlStyle> ControlStyles;
        List<DecorItem> BgndDecorList;
        List<DecorItem> FgndDecorList;
        ResourceManager Res;
        IPlugin Plugin;
        Control mTopLevelControl = null;
        public ButtonStyle DefaultButtonStyle;
        public ControlStyle DefaultControlStyle; 


        public GuiConfig()
        {
            BgndDecorList = new List<DecorItem>();
            FgndDecorList = new List<DecorItem>();
            Controls = new Dictionary<string, ctlUserPanel>();
            Buttons = new Dictionary<string, ctlImageButton>();
            ControlStyles = new Dictionary<string, ControlStyle>();
            Res = global::UV_DLP_3D_Printer.Properties.Resources.ResourceManager;
            Plugin = null;
            DefaultButtonStyle = new ButtonStyle();
            DefaultButtonStyle.Name = "DefaultButton";
            DefaultButtonStyle.SetDefault();
            DefaultControlStyle = new ControlStyle();
            DefaultControlStyle.Name = "DefaultControl";
            DefaultControlStyle.SetDefault();
            ControlStyles[DefaultButtonStyle.Name] = DefaultButtonStyle;
            ControlStyles[DefaultControlStyle.Name] = DefaultControlStyle;
        }

        public Control TopLevelControl
        {
            get { return mTopLevelControl; }
            set { mTopLevelControl = value; }
        }

        public void AddControl(string name, ctlUserPanel ctl)
        {
            Controls[name] = ctl;
            if ((ctl.Parent == null) && (mTopLevelControl != null))
                mTopLevelControl.Controls.Add(ctl);
        }

        public void AddButton(string name, ctlImageButton ctl)
        {
            Buttons[name] = ctl;
            if ((ctl.Parent == null) && (mTopLevelControl != null))
                mTopLevelControl.Controls.Add(ctl);
        }

        public ButtonStyle GetButtonStyle(string name)
        {
            if ((name == null) || !ControlStyles.ContainsKey(name))
                return null;
            ControlStyle ct = ControlStyles[name];
            if (ct.GetType() == typeof(ButtonStyle))
                return (ButtonStyle)ct;
            return null;
        }

        public ControlStyle GetControlStyle(string name)
        {
            if ((name == null) || !ControlStyles.ContainsKey(name))
                return null;
            return ControlStyles[name];
        }


        public ctlUserPanel GetControl(string name)
        {
            if (!Controls.ContainsKey(name))
                return null;
            return Controls[name];
        }

        public ctlImageButton GetButton(string name)
        {
            if (!Buttons.ContainsKey(name))
                return null;
            return Buttons[name];
        }

        public static int GetPosition(int refpos, int refwidth, int width, int gap, Char anchor)
        {
            int retval = 0;
            switch (anchor)
            {
                case 't':
                case 'l':
                    retval = refpos + gap;
                    break;

                case 'c':
                    retval = refpos + (refwidth - width) / 2 + gap;
                    break;

                case 'r':
                case 'b':
                    retval = refpos + refwidth - width - gap;
                    break;
                default:
                    retval = gap;
                    break;
            }
            return retval;
        }

        public void LoadConfiguration(String xmlConf, IPlugin plugin)
        {
            Plugin = plugin;
            try
            {
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xmlConf));
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(ms);
                XmlNode rootnode = xdoc.ChildNodes[1];
                if (rootnode.Name != "GuiConfig")
                    return;

                foreach (XmlNode xnode in rootnode.ChildNodes)
                {
                    switch (xnode.Name)
                    {
                        case "decals": HandleDecals(xnode); break;
                        case "buttons": HandleButtons(xnode); break;
                        case "controls": HandleControls(xnode); break;
                    }
                }

            }
            catch (Exception) { }
        }

        public void LoadConfiguration(String xmlConf)
        {
            LoadConfiguration(xmlConf, null);
        }

        #region Decals

        void HandleDecals(XmlNode decalnode)
        {
            if (GetBoolParam(decalnode, "HideAll", false))
                ClearLayout();
            foreach (XmlNode xnode in decalnode.ChildNodes)
            {
                switch (xnode.Name)
                {
                    case "bar": HandleBars(xnode); break;
                    case "image": HandleImages(xnode); break;
                }
            }
        }

        List<DecorItem> GetListFromLevel(XmlNode xnode)
        {
            List<DecorItem> dlist = FgndDecorList;
            if (GetStrParam(xnode, "level", "") == "background")
            {
                dlist = BgndDecorList;
            }
            return dlist;
        }


        void HandleBars(XmlNode barnode)
        {
            string docking = GetStrParam(barnode, "dock", "n").ToLower();
            int w = GetIntParam(barnode, "width", 100);
            List<DecorItem> dlist = GetListFromLevel(barnode);
            if (GetStrParam(barnode, "color", null) == null)
            {
                Color coltl = GetColorParam(barnode, "tlcolor", Color.White);
                Color coltr = GetColorParam(barnode, "trcolor", Color.White);
                Color colbl = GetColorParam(barnode, "blcolor", Color.White);
                Color colbr = GetColorParam(barnode, "brcolor", Color.White);
                dlist.Add(new DecorBar(docking, w, coltl, coltr, colbl, colbr));
            }
            else
            {
                Color col = GetColorParam(barnode, "color", Color.White);
                dlist.Add(new DecorBar(docking, w, col));
            }
        }

        void HandleImages(XmlNode imgnode)
        {
            string name = GetStrParam(imgnode, "name", null);
            if (name == null)
                return;
            string docking = FixDockingVal(GetStrParam(imgnode, "dock", "cc"));
            int x = GetIntParam(imgnode, "x", 0);
            int y = GetIntParam(imgnode, "y", 0);
            Color col = GetColorParam(imgnode, "color", Color.White);
            int opacity = GetIntParam(imgnode, "opacity", -1) * 255 / 100;
            if ((opacity >= 0) && (opacity <= 255))
                col = Color.FromArgb(opacity, col.R, col.G, col.B);
            List<DecorItem> dlist = GetListFromLevel(imgnode);
            dlist.Add(new DecorImage(name, docking, x, y, col));
        }
        #endregion

        #region Buttons
        void HandleButtons(XmlNode buttnode)
        {
            if (GetBoolParam(buttnode, "HideAll", false))
                HideAllButtons();
            foreach (XmlNode xnode in buttnode.ChildNodes)
            {
                switch (xnode.Name)
                {
                    case "style": HandleButtonStyle(xnode); break;
                    case "button": HandleButton(xnode); break;
                }
            }
        }

        void HandleButtonStyle(XmlNode xnode)
        {
            string name = GetStrParam(xnode, "name", "DefaultButton");
            ButtonStyle bt = GetButtonStyle(name);
            if (bt == null)
            {
                bt = new ButtonStyle();
                bt.Name = name;
                ControlStyles[name] = bt;
                bt.SetDefault();
            }
            UpdateStyle(xnode, bt);
            bt.CheckedColor = GetColorParam(xnode, "checkcolor", bt.CheckedColor);
            bt.HoverColor = GetColorParam(xnode, "hovercolor", bt.HoverColor);
            bt.PressedColor = GetColorParam(xnode, "presscolor", bt.PressedColor);
            bt.SubImgCount = GetIntParam(xnode, "nimages", bt.SubImgCount);
            bt.BackImage = GetStrParam(xnode, "backimage", bt.BackImage);
            bt.CheckedImage = GetStrParam(xnode, "checkimage", bt.CheckedImage);
            bt.DisabledColor = GetColorParam(xnode, "disablecolor", bt.DisabledColor);

            if (name == "DefaultButton")
            {
                foreach (KeyValuePair<String, ctlImageButton> pair in Buttons)
                {
                    ctlImageButton butt = pair.Value;
                    butt.ApplyStyle(DefaultButtonStyle);
                }
            }
        }

        void HandleButton(XmlNode buttnode)
        {
            string name = GetStrParam(buttnode, "name", null);
            if (name == null)
                return;
            if (!Buttons.ContainsKey(name))
            {
                // create a new empty button
                AddButton(name, new ctlImageButton());
                Buttons[name].BringToFront();
            }
            ctlImageButton butt = Buttons[name];
            butt.Visible = true;
            butt.GuiAnchor = FixDockingVal(GetStrParam(buttnode, "dock", butt.GuiAnchor));
            butt.Gapx = GetIntParam(buttnode, "x", butt.Gapx);
            butt.Gapy = GetIntParam(buttnode, "y", butt.Gapy);
            butt.Width = GetIntParam(buttnode, "w", butt.Width);
            butt.Height = GetIntParam(buttnode, "h", butt.Height);
            butt.StyleName = GetStrParam(buttnode, "style", butt.StyleName);
            butt.OnClickCallback = GetStrParam(buttnode, "click", butt.OnClickCallback);
            ButtonStyle bstl = GetButtonStyle(butt.StyleName);
            if (bstl != null)
            {
                butt.GLVisible = bstl.glMode;
            }
            //butt.GLVisible = GetBoolParam(buttnode, "gl", butt.GLVisible);
            if (butt.GLVisible)
                butt.GLImage = GetStrParam(buttnode, "image", null);
            else
                butt.Image = GetImageParam(buttnode, "image", butt.Image);
            butt.CheckImage = GetImageParam(buttnode, "check", butt.CheckImage);
        }

        #endregion

        #region Controls
        void HandleControls(XmlNode ctlnode)
        {
            if (GetBoolParam(ctlnode, "HideAll", false))
                HideAllControls();
            foreach (XmlNode xnode in ctlnode.ChildNodes)
            {
                switch (xnode.Name)
                {
                    case "style": HandleControlStyle(xnode); break;
                    case "control": HandleControl(xnode); break;
                }
            }
        }
        
        void HandleControlStyle(XmlNode xnode)
        {
            string name = GetStrParam(xnode, "name", "DefaultControl");
            ControlStyle ct = GetControlStyle(name);
            if (ct == null)
            {
                ct = new ControlStyle();
                ct.Name = name;
                ControlStyles[name] = ct;
                ct.SetDefault();
            }
            UpdateStyle(xnode, ct);
            if (name == "DefaultControl")
            {
                foreach (KeyValuePair<String, ctlUserPanel> pair in Controls)
                {
                    ctlUserPanel ctl = pair.Value;
                    ctl.ApplyStyle(DefaultControlStyle);
                }
            }
        }

        void HandleControl(XmlNode ctlnode)
        {
            string name = GetStrParam(ctlnode, "name", null);
            if (name == null)
                return;
            if (!Controls.ContainsKey(name))
                return;
            ctlUserPanel ctl = Controls[name];
            ctl.GuiAnchor = FixDockingVal(GetStrParam(ctlnode, "dock", ctl.GuiAnchor));
            ctl.Gapx = GetIntParam(ctlnode, "x", ctl.Gapx);
            ctl.Gapy = GetIntParam(ctlnode, "y", ctl.Gapy);
            ctl.Width = GetIntParam(ctlnode, "w", ctl.Width);
            ctl.Height = GetIntParam(ctlnode, "h", ctl.Height);
            ctl.StyleName = GetStrParam(ctlnode, "style", ctl.StyleName);
            ControlStyle bstl = GetControlStyle(ctl.StyleName);
            if (bstl != null)
            {
                ctl.GLVisible = bstl.glMode;
            }
            //ctl.GLVisible = GetBoolParam(ctlnode, "gl", false);
            if (ctl.GLVisible)
                ctl.GLBackgroundImage = GetStrParam(ctlnode, "shape", ctl.GLBackgroundImage);
            else
                ctl.bgndPanel.imageName = GetStrParam(ctlnode, "shape", ctl.bgndPanel.imageName);
        }

        #endregion


        #region Attribute parsing
        string GetStrParam(XmlNode xnode, string paramName, string defVal)
        {
            try
            {
                string res = xnode.Attributes[paramName].Value;
                return res;
            }
            catch (Exception)
            {
                return defVal;
            }
        }

        int GetIntParam(XmlNode xnode, string paramName, int defVal)
        {
            try
            {
                int res = int.Parse(xnode.Attributes[paramName].Value);
                return res;
            }
            catch (Exception)
            {
                return defVal;
            }
        }

        bool GetBoolParam(XmlNode xnode, string paramName, bool defVal)
        {
            try
            {
                bool res = bool.Parse(xnode.Attributes[paramName].Value);
                return res;
            }
            catch (Exception)
            {
                return defVal;
            }
        }

        Color GetColorParam(XmlNode xnode, string paramName, Color defVal)
        {
            Color res;
            try
            {
                string sres = xnode.Attributes[paramName].Value;
                if (sres[0] == '#')
                {
                    sres = sres.Substring(1);
                    if (sres.Length > 7)
                        res = Color.FromArgb(int.Parse(sres, System.Globalization.NumberStyles.HexNumber));
                    else
                        res = Color.FromArgb((int)(long.Parse(sres, System.Globalization.NumberStyles.HexNumber) | 0xFF000000));
                }
                else
                {
                    res = Color.FromName(sres);
                }
                return res;
            }
            catch (Exception)
            {
                return defVal;
            }

        }

        Image GetImageParam(XmlNode xnode, string paramName, Image defVal)
        {
            string imgname = GetStrParam(xnode, paramName, null);
            if (imgname == null)
                return defVal;
            Image img = null;
            if (Plugin != null)
                img = Plugin.GetImage(imgname);
            if (img == null)
                img = (Image)Res.GetObject(imgname);
            if (img == null)
                return defVal;
            return img;
        }

        string FixDockingVal(string origdock)
        {
            if (origdock == null)
                return "cc";
            string dock = origdock.ToLower();
            while (dock.Length < 2)
                dock += "c";
            return dock;
        }

        void UpdateStyle(XmlNode xnode, ControlStyle ct)
        {
            ct.ForeColor = GetColorParam(xnode, "forecolor", ct.ForeColor);
            ct.BackColor = GetColorParam(xnode, "backcolor", ct.BackColor);
            ct.FrameColor = GetColorParam(xnode, "framecolor", ct.BackColor);
            ct.glMode = GetBoolParam(xnode, "gl", ct.glMode);
        }

        #endregion

        #region Perform layout

        void Draw(List<DecorItem> dlist, C2DGraphics g2d, int w, int h)
        {
            foreach (DecorItem di in dlist)
            {
                di.Show(g2d, w, h);
            }
        }

        public void DrawForeground(C2DGraphics g2d, int w, int h)
        {
            Draw(FgndDecorList, g2d, w, h);
        }

        public void DrawBackground(C2DGraphics g2d, int w, int h)
        {
            Draw(BgndDecorList, g2d, w, h);
        }

        public void LayoutGui(int w, int h)
        {
            LayoutButtons(w, h);
            LayoutControls(w, h);
        }

        void LayoutButtons(int w, int h)
        {
            foreach (KeyValuePair<String, ctlImageButton> pair in Buttons)
            {
                ctlImageButton butt = pair.Value;
                if (butt.GuiAnchor == null)
                    continue;
                int px = GetPosition(0, w, butt.Width, butt.Gapx, butt.GuiAnchor[1]);
                int py = GetPosition(0, h, butt.Height, butt.Gapy, butt.GuiAnchor[0]);
                butt.Location = new System.Drawing.Point(px, py);
            }
        }
        
        void LayoutControls(int w, int h)
        {
            foreach (KeyValuePair<String, ctlUserPanel> pair in Controls)
            {
                ctlUserPanel ctl = pair.Value;
                if (ctl.GuiAnchor == null)
                    continue;
                int px = GetPosition(0, w, ctl.Width, ctl.Gapx, ctl.GuiAnchor[1]);
                int py = GetPosition(0, h, ctl.Height, ctl.Gapy, ctl.GuiAnchor[0]);
                ctl.Location = new System.Drawing.Point(px, py);
            }
        }

        public void ClearLayout()
        {
            BgndDecorList = new List<DecorItem>();
            FgndDecorList = new List<DecorItem>();
        }

        public void HideAllButtons()
        {
            foreach (KeyValuePair<String, ctlImageButton> pair in Buttons)
            {
                ctlImageButton butt = pair.Value;
                butt.Visible = false;
            }
        }

        void HideAllControls()
        {
            foreach (KeyValuePair<String, ctlUserPanel> pair in Controls)
            {
                ctlUserPanel ctl = pair.Value;
                ctl.Visible = false;
            }
        }

        #endregion
    }
}
