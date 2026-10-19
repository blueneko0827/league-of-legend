using UnityEngine;
namespace MidLane
{
    public sealed partial class PracticeHud
    {
        bool runePicker;
        public bool RunePickerOpen { get { return runePicker&&!ShopHud.Open&&!world.Paused; } }
        public Rect RunePickerRect { get { return new Rect(Mathf.Clamp(ChampionHudLayout.Runes.center.x-118,12,1032),ChampionHudLayout.Body.yMin-254,236,244); } }
        public Rect RuneChoiceRect(int choice) { var p=RunePickerRect; return new Rect(p.x+8,p.y+36+choice*66,p.width-16,60); }
        public void ToggleRunePicker()
        {
            if(ShopHud.Open||world.Paused) return;
            runePicker=!runePicker; CloseSummonerPicker();
            if(runePicker) world.GetComponent<PlayerCommands>().CancelForRunePicker();
        }
        public void CloseRunePicker() { runePicker=false; }
        public bool SelectRuneChoice(IreliaRunePage page)
        {
            if(!RunePickerOpen||page==world.SelectedRunePage) return false;
            CloseRunePicker(); return world.SelectRunePage(page);
        }
        public bool RunePickerContainsScreenPoint(Vector2 topLeftScreen)
        {
            Vector2 p=new Vector2(topLeftScreen.x*1280/Screen.width,topLeftScreen.y*720/Screen.height);
            return RunePickerRect.Contains(p)||ChampionHudLayout.Runes.Contains(p);
        }
        static string SecondaryRuneIcon(IreliaRunePage page)
        { return page==IreliaRunePage.Resolve?"RuneResolve":page==IreliaRunePage.Domination?"RuneDomination":"RuneInspiration"; }
        void DrawRuneButton()
        {
            Rect r=new Rect(12,150,76,40); Box(r,HudInk); HudFrame(r,HudGold,2);
            // Pixel strokes avoid missing Unicode glyphs and rotating an IMGUI clipping matrix.
            for(int side=0;side<2;side++)
            {
                float start=side*180+25;
                Vector2 previous=r.center+new Vector2(Mathf.Cos(start*Mathf.Deg2Rad),Mathf.Sin(start*Mathf.Deg2Rad))*13;
                for(int angle=5;angle<=130;angle+=5)
                {
                    float a=(start+angle)*Mathf.Deg2Rad;
                    Vector2 next=r.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*13;
                    RuneStroke(previous,next,HudBright); previous=next;
                }
                float end=(start+130)*Mathf.Deg2Rad;
                Vector2 tangent=new Vector2(-Mathf.Sin(end),Mathf.Cos(end)),radial=new Vector2(Mathf.Cos(end),Mathf.Sin(end));
                RuneStroke(previous,previous-tangent*7+radial*5,HudBright);
                RuneStroke(previous,previous-tangent*7-radial*5,HudBright);
            }
            if(GUI.Button(r,new GUIContent("","Cambiar runas · reinicia la partida"),GUIStyle.none)) ToggleRunePicker();
        }
        static void RuneStroke(Vector2 from,Vector2 to,Color color)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)));
            for(int i=0;i<=steps;i++) { Vector2 p=Vector2.Lerp(from,to,i/(float)steps); Box(new Rect(p.x,p.y,2,2),color); }
        }
        static void RuneRing(Rect r,Color color)
        {
            Vector2 previous=r.center+Vector2.right*(r.width*.5f);
            for(int angle=6;angle<=360;angle+=6)
            {
                float a=angle*Mathf.Deg2Rad; Vector2 next=r.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(r.width*.5f);
                RuneStroke(previous,next,color); previous=next;
            }
        }
        void DrawRunePicker()
        {
            if(ShopHud.Open||world.Paused) { CloseRunePicker(); return; }
            if(!RunePickerOpen) return;
            Rect panel=RunePickerRect; Box(panel,HudInk); HudFrame(panel,HudGold,1);
            GUI.Label(new Rect(panel.x+12,panel.y+6,180,24),"Runas · reinicia partida",small);
            if(GUI.Button(new Rect(panel.xMax-30,panel.y+6,22,22),"×",button)) { CloseRunePicker(); return; }
            bool enabled=GUI.enabled;
            for(int i=0;i<3;i++)
            {
                var page=(IreliaRunePage)i; Rect r=RuneChoiceRect(i); bool selected=page==world.SelectedRunePage;
                Box(r,selected?new Color(.025f,.045f,.055f):new Color(.04f,.10f,.13f));
                GUI.enabled=enabled&&!selected;
                bool clicked=GUI.Button(r,new GUIContent("", "Precisión + "+(i==0?"Valor":i==1?"Dominación":"Inspiración")),GUIStyle.none);
                Color tint=GUI.color; if(selected) GUI.color=new Color(.4f,.4f,.4f);
                RuneRing(new Rect(r.center.x-59,r.y+4,52,52),HudGold);
                RuneRing(new Rect(r.center.x+7,r.y+4,52,52),i==0?new Color(.45f,.7f,.35f):i==1?new Color(.8f,.25f,.25f):new Color(.25f,.65f,.7f));
                HudIcons.Draw(new Rect(r.center.x-54,r.y+9,42,42),"RunePrecision");
                HudIcons.Draw(new Rect(r.center.x+12,r.y+9,42,42),SecondaryRuneIcon(page));
                GUI.color=tint; GUI.enabled=enabled;
                if(clicked) { SelectRuneChoice(page); return; }
            }
        }
    }
}
