﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UV_DLP_3D_Printer._3DEngine;

namespace UV_DLP_3D_Printer.GUI.CustomGUI
{
    public partial class ctlImageButton : ctlAnchorable
    { 
        Image mImage;
        Image mCheckImage;
        Rectangle mDstrc;
        Rectangle mSrcrc;
        Rectangle mCheckrc;
        int nSubImages = 4;
        int mSubImgWidth, mSubChkImgWidth;
        String mGLImage;
        C2DImage mGLImageCach;
        ButtonStyle mButtStyle;
        String mOnClickCallback = null;


        [Description("Image composesed of all 4 button states"), Category("Data")]
        public Image Image
        {
            get { return mImage; }
            set
            {
                mImage = value;
                if (mImage != null)
                {
                    mSubImgWidth = mImage.Width / nSubImages;
                    mSrcrc = new Rectangle(0, 0, mSubImgWidth, mImage.Height);
                    ScaleImage();
                }
            }
        }

        [Description("Image of check/uncheck mark"), Category("Data")]
        public Image CheckImage
        {
            get { return mCheckImage; }
            set
            {
                mCheckImage = value;
                if (mCheckImage != null)
                {
                    mSubChkImgWidth = mCheckImage.Width / 2;
                    mCheckrc = new Rectangle(0, 0, mSubChkImgWidth, mCheckImage.Height);
                }
            }
        }

        [Description("GL image name"), Category("Data")]
        public String GLImage
        {
            get { return mGLImage; }
            set
            {
                mGLImage = value;
                mGLImageCach = null;
            }
        }

        [DefaultValue(null)]
        [Description("On Click callback command name"), Category("Data")]
        public String OnClickCallback
        {
            get { return mOnClickCallback; }
            set { mOnClickCallback = value; }
        }
        
        public ButtonStyle ButtStyle
        {
            get
            {
                if (mButtStyle == null)
                    mButtStyle = UVDLPApp.Instance().m_gui_config.GetButtonStyle(mStyleName);
                if (mButtStyle == null)
                    return UVDLPApp.Instance().m_gui_config.DefaultButtonStyle;
                return mButtStyle;
            }
        }


        public ctlImageButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            InitializeComponent();
        }

        void ScaleImage()
        {
            if (mImage == null)
                return;
            if ((Height == 0) || (Width == 0))
                return;
            float iratio = (float)mSubImgWidth / (float)Image.Height;
            float cratio = (float)Width / (float)Height;
            if (iratio > cratio)
            {
                int h = (int)((float)Width / iratio);
                mDstrc = new Rectangle(0, (Height - h) / 2, Width, h);
            }
            else
            {
                int w = (int)((float)Height * iratio);
                mDstrc = new Rectangle((Width - w) / 2, 0, w, Height);
            }
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ScaleImage();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (mGLVisible)
                return;
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            if (mGLVisible)
            {
                base.OnPaint(pevent);
                return;
            }
            Graphics gr = pevent.Graphics;
            int index = (int)mCtlState;
            if (Enabled == false)
                index = 3;
            if (mImage != null)
            {
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                mSrcrc.X = mSubImgWidth * index;
                gr.DrawImage(mImage, mDstrc, mSrcrc, GraphicsUnit.Pixel);
                if (Enabled && (mCheckImage != null))
                {
                    mCheckrc.X = Checked ? mSubChkImgWidth : 0;
                    gr.DrawImage(mCheckImage, mDstrc, mCheckrc, GraphicsUnit.Pixel);
                }
            }
            //base.OnPaint(pevent);
        }

        public override void ApplyStyle(ControlStyle ct)
        {
            base.ApplyStyle(ct);
            if (ct.ForeColor != ControlStyle.NullColor)
                ForeColor = ct.ForeColor;
            if (ct.BackColor != ControlStyle.NullColor)
                BackColor = ct.BackColor;
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            OnClick(e);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (mOnClickCallback == null)
                return;
            UVDLPApp.Instance().m_callbackhandler.Activate(mOnClickCallback, this);
        }

        private void InitializeComponent()
        {
        }

        void GLPaint4(C2DGraphics gr)
        {
            int index = (int)mCtlState;
            if (Enabled == false)
                index = 3;
            if (mImage != null)
            {
                gr.SetColor(Color.White);
                mSrcrc.X = mSubImgWidth * index;
                gr.Image(mGLImageCach, mSubImgWidth * index, 0, mSubImgWidth, mGLImageCach.h, 0, 0, Width, Height);
                /*if (Enabled && (mCheckImage != null))
                {
                    mCheckrc.X = Checked ? mSubChkImgWidth : 0;
                    gr.DrawImage(mCheckImage, mDstrc, mCheckrc, GraphicsUnit.Pixel);
                }*/
            }
        }

        void GLPaint1(C2DGraphics gr, ButtonStyle stl)
        {
            if (Enabled == false)
            {
                gr.SetColor(stl.DisabledColor);
            }
            else
            {
                switch (mCtlState)
                {
                    case CtlState.Hover:
                        if (Checked)
                            gr.SetColor(stl.CheckedColor);
                        else
                            gr.SetColor(stl.HoverColor);
                        break;

                    case CtlState.Normal:
                        if (Checked)
                            gr.SetColor(stl.CheckedColor);
                        else
                            gr.SetColor(stl.ForeColor);
                        break;

                    case CtlState.Pressed:
                        gr.SetColor(stl.PressedColor);
                        break;
                }
            }

            if (mCtlState == CtlState.Hover)
            {
                float scx = (float)Width / 16f;
                float scy = (float)Height / 16f;
                gr.Image(mGLImageCach, 0, 0, mGLImageCach.w, mGLImageCach.h, -scx, -scy, Width + 2f * scx, Height + 2f * scy);
            }
            else
            {
                gr.Image(mGLImageCach, 0, 0, mGLImageCach.w, mGLImageCach.h, 0, 0, Width, Height);
            }
        }

        public override void OnGLPaint(C2DGraphics gr)
        {
            base.OnGLPaint(gr);
            if (mGLImageCach == null)
            {
                mGLImageCach = gr.GetImage(mGLImage);
                if (mGLImageCach == null)
                    return;
                mSubImgWidth = mGLImageCach.w / nSubImages;
            }
            ButtonStyle stl = ButtStyle;
            if (stl.SubImgCount == 4)
                GLPaint4(gr);
            if (stl.SubImgCount == 1)
                GLPaint1(gr, stl);
            C2DImage cimg = stl.CheckedImageCach;
            if (Enabled && (cimg != null))
            {
                int chimgw = cimg.w / 2;
                int posx = Checked ? chimgw : 0;
                gr.SetColor(stl.CheckedColor);
                gr.Image(cimg, posx, 0, chimgw, cimg.h, 0, 0, Width, Height);
            }
        }
    }
}
