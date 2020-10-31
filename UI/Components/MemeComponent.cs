using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiveSplit.Model.Input;

namespace LiveSplit.UI.Components
{
    public class MemeComponent : IComponent
    {
        public MemeComponent(LiveSplitState state)
        {
            VerticalHeight = 10;
            Cache = new GraphicsCache();
            TextLabel = new SimpleLabel();
            TextLabel.Text = "lol";
            VerticalHeight = 10;
            this.state = state;

            memeHooks = new MemeHooks();
            //memeHooks.targetSpeed = 2.0f;
            lastSplitIndex = state.CurrentSplitIndex;

        }

        public GraphicsCache Cache { get; set; }

        public float VerticalHeight { get; set; }

        public float MinimumHeight { get; set; }

        public float MinimumWidth 
        {
            get;set;
        }

        public float HorizontalWidth { get; set; }

        public IDictionary<string, Action> ContextMenuControls
        {
            get { return null; }
        }

        public float PaddingTop { get; set; }
        public float PaddingLeft { get { return 7f; } }
        public float PaddingBottom { get; set; }
        public float PaddingRight { get { return 7f; } }

        protected SimpleLabel TextLabel = new SimpleLabel();

        protected Font CounterFont { get; set; }

        private LiveSplitState state;

        private MemeHooks memeHooks;

        private void DrawGeneral(Graphics g, Model.LiveSplitState state, float width, float height, LayoutMode mode)
        {
            // Calculate Height from Font.
            var textHeight = g.MeasureString("A", state.LayoutSettings.TextFont).Height;
            VerticalHeight = 1.2f * textHeight;
            MinimumHeight = MinimumHeight;

            TextLabel.Width = width;
            TextLabel.Height = height;
            TextLabel.Font = state.LayoutSettings.TextFont;
            TextLabel.Brush = new SolidBrush(state.LayoutSettings.TextColor);




            TextLabel.Draw(g);
        }

        public void DrawHorizontal(Graphics g, Model.LiveSplitState state, float height, Region clipRegion)
        {
            DrawGeneral(g, state, HorizontalWidth, height, LayoutMode.Horizontal);
        }

        public void DrawVertical(System.Drawing.Graphics g, Model.LiveSplitState state, float width, Region clipRegion)
        {
            DrawGeneral(g, state, width, VerticalHeight, LayoutMode.Vertical);
        }

        public string ComponentName
        {
            get { return "HP2 Meme"; }
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            return null;
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return null;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            
        }

        private int lastSplitIndex = -1;

        public void Update(IInvalidator invalidator, Model.LiveSplitState state, float width, float height, LayoutMode mode)
        {

            this.state = state;
            if (state.CurrentSplitIndex == 0)
            {
                memeHooks.targetSpeed = 1.0f;
                lastSplitIndex = 0;
            }
            else if (state.CurrentSplitIndex != lastSplitIndex)
            {
                lastSplitIndex = state.CurrentSplitIndex;
                var lastSplitComparison = state.Run[state.CurrentSplitIndex - 1].Comparisons[state.CurrentComparison][state.CurrentTimingMethod];
                var lastSplitTime = state.Run[state.CurrentSplitIndex - 1].SplitTime[state.CurrentTimingMethod];
                if (lastSplitTime > lastSplitComparison)
                {
                    memeHooks.targetSpeed *= 1.1f;
                } else
                {
                    memeHooks.targetSpeed *= 0.9f;
                }

            }
            
            
            TextLabel.Text = memeHooks.state;

            Cache.Restart();
            Cache["Text"] = TextLabel.Text;
            

            if (invalidator != null && Cache.HasChanged)
            {
                invalidator.Invalidate(0, 0, width, height);
            }
        }

        public void Dispose()
        {
            
        }

        public int GetSettingsHashCode()
        {
            return 0;
        }
    }
}
