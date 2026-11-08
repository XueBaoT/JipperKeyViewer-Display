using JipperKeyViewer.Config;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{
    private void LoadRainValues(AppConfig c)
    {

        chkRainEnable.IsChecked = c.RainEnabled;
        chkRainR1.IsChecked = c.EnableRainForRow1;
        chkRainR2.IsChecked = c.EnableRainForRow2;
        chkRainR3.IsChecked = c.EnableRainForRow3;
        SetArr(sldRainSp1, valRainSp1, c.RainSpeedRow1, "F0");
        SetArr(sldRainSp2, valRainSp2, c.RainSpeedRow2, "F0");
        SetArr(sldRainSp3, valRainSp3, c.RainSpeedRow3, "F0");
        SetArr(sldRainH1, valRainH1, c.RainHeightRow1, "F0");
        SetArr(sldRainH2, valRainH2, c.RainHeightRow2, "F0");
        SetArr(sldRainH3, valRainH3, c.RainHeightRow3, "F0");
        SetArr(sldRainW1, valRainW1, c.RainWidthRow1, "F0");
        SetArr(sldRainW2, valRainW2, c.RainWidthRow2, "F0");
        SetArr(sldRainW3, valRainW3, c.RainWidthRow3, "F0");
        SetArr(sldRainY1, valRainY1, c.RainStartYRow1, "F0");
        SetArr(sldRainY2, valRainY2, c.RainStartYRow2, "F0");
        SetArr(sldRainY3, valRainY3, c.RainStartYRow3, "F0");
        chkRainFade.IsChecked = c.EnableRainFade;
        chkRainGrad.IsChecked = c.EnableRainGradient;
        sldRainFadeT.Value = c.RainFadeDuration * 100;
        sldRainFadePx.Value = c.RainFadePx;

        chkRainSh1.IsChecked = c.EnableRainShadowRow1;
        chkRainSh2.IsChecked = c.EnableRainShadowRow2;
        chkRainSh3.IsChecked = c.EnableRainShadowRow3;
        sldRainShX1.Value = c.RainShadowOffsetXRow1; valRainShX1.Text = c.RainShadowOffsetXRow1.ToString("F0");
        sldRainShY1.Value = c.RainShadowOffsetYRow1; valRainShY1.Text = c.RainShadowOffsetYRow1.ToString("F0");
        sldRainShX2.Value = c.RainShadowOffsetXRow2; valRainShX2.Text = c.RainShadowOffsetXRow2.ToString("F0");
        sldRainShY2.Value = c.RainShadowOffsetYRow2; valRainShY2.Text = c.RainShadowOffsetYRow2.ToString("F0");
        sldRainShX3.Value = c.RainShadowOffsetXRow3; valRainShX3.Text = c.RainShadowOffsetXRow3.ToString("F0");
        sldRainShY3.Value = c.RainShadowOffsetYRow3; valRainShY3.Text = c.RainShadowOffsetYRow3.ToString("F0");
        chkRainOl1.IsChecked = c.EnableRainOutlineRow1;
        chkRainOl2.IsChecked = c.EnableRainOutlineRow2;
        chkRainOl3.IsChecked = c.EnableRainOutlineRow3;
        sldRainOlW1.Value = c.RainOutlineWidthRow1; valRainOlW1.Text = c.RainOutlineWidthRow1.ToString("F1");
        sldRainOlW2.Value = c.RainOutlineWidthRow2; valRainOlW2.Text = c.RainOutlineWidthRow2.ToString("F1");
        sldRainOlW3.Value = c.RainOutlineWidthRow3; valRainOlW3.Text = c.RainOutlineWidthRow3.ToString("F1");

        chkGhostRain.IsChecked = c.EnableGhostRain;
        sldGhostY1.Value = c.GhostRainStartYRow1; valGhostY1.Text = c.GhostRainStartYRow1.ToString("F0");
        sldGhostY2.Value = c.GhostRainStartYRow2; valGhostY2.Text = c.GhostRainStartYRow2.ToString("F0");
        sldGhostY3.Value = c.GhostRainStartYRow3; valGhostY3.Text = c.GhostRainStartYRow3.ToString("F0");
        chkGRSh1.IsChecked = c.EnableGhostRainShadowRow1;
        chkGRSh2.IsChecked = c.EnableGhostRainShadowRow2;
        chkGRSh3.IsChecked = c.EnableGhostRainShadowRow3;
        sldGRShX1.Value = c.GhostRainShadowOffsetXRow1; valGRShX1.Text = c.GhostRainShadowOffsetXRow1.ToString("F0");
        sldGRShY1.Value = c.GhostRainShadowOffsetYRow1; valGRShY1.Text = c.GhostRainShadowOffsetYRow1.ToString("F0");
        sldGRShX2.Value = c.GhostRainShadowOffsetXRow2; valGRShX2.Text = c.GhostRainShadowOffsetXRow2.ToString("F0");
        sldGRShY2.Value = c.GhostRainShadowOffsetYRow2; valGRShY2.Text = c.GhostRainShadowOffsetYRow2.ToString("F0");
        sldGRShX3.Value = c.GhostRainShadowOffsetXRow3; valGRShX3.Text = c.GhostRainShadowOffsetXRow3.ToString("F0");
        sldGRShY3.Value = c.GhostRainShadowOffsetYRow3; valGRShY3.Text = c.GhostRainShadowOffsetYRow3.ToString("F0");
        chkGROl1.IsChecked = c.EnableGhostRainOutlineRow1;
        chkGROl2.IsChecked = c.EnableGhostRainOutlineRow2;
        chkGROl3.IsChecked = c.EnableGhostRainOutlineRow3;
        sldGROlW1.Value = c.GhostRainOutlineWidthRow1; valGROlW1.Text = c.GhostRainOutlineWidthRow1.ToString("F1");
        sldGROlW2.Value = c.GhostRainOutlineWidthRow2; valGROlW2.Text = c.GhostRainOutlineWidthRow2.ToString("F1");
        sldGROlW3.Value = c.GhostRainOutlineWidthRow3; valGROlW3.Text = c.GhostRainOutlineWidthRow3.ToString("F1");

    }

    private void SaveRainValues(AppConfig c)
    {

        c.RainEnabled = chkRainEnable.IsChecked == true;
        c.EnableRainForRow1 = chkRainR1.IsChecked == true;
        c.EnableRainForRow2 = chkRainR2.IsChecked == true;
        c.EnableRainForRow3 = chkRainR3.IsChecked == true;
        c.RainSpeedRow1 = (float)sldRainSp1.Value; c.RainSpeedRow2 = (float)sldRainSp2.Value; c.RainSpeedRow3 = (float)sldRainSp3.Value;
        c.RainHeightRow1 = (float)sldRainH1.Value; c.RainHeightRow2 = (float)sldRainH2.Value; c.RainHeightRow3 = (float)sldRainH3.Value;
        c.RainWidthRow1 = (float)sldRainW1.Value; c.RainWidthRow2 = (float)sldRainW2.Value; c.RainWidthRow3 = (float)sldRainW3.Value;
        c.RainStartYRow1 = (float)sldRainY1.Value; c.RainStartYRow2 = (float)sldRainY2.Value; c.RainStartYRow3 = (float)sldRainY3.Value;
        c.EnableRainFade = chkRainFade.IsChecked == true;
        c.EnableRainGradient = chkRainGrad.IsChecked == true;
        c.RainFadeDuration = (float)(sldRainFadeT.Value / 100.0);
        c.RainFadePx = (float)sldRainFadePx.Value;

        c.EnableRainShadowRow1 = chkRainSh1.IsChecked == true;
        c.EnableRainShadowRow2 = chkRainSh2.IsChecked == true;
        c.EnableRainShadowRow3 = chkRainSh3.IsChecked == true;
        c.RainShadowOffsetXRow1 = (float)sldRainShX1.Value; c.RainShadowOffsetYRow1 = (float)sldRainShY1.Value;
        c.RainShadowOffsetXRow2 = (float)sldRainShX2.Value; c.RainShadowOffsetYRow2 = (float)sldRainShY2.Value;
        c.RainShadowOffsetXRow3 = (float)sldRainShX3.Value; c.RainShadowOffsetYRow3 = (float)sldRainShY3.Value;
        c.EnableRainOutlineRow1 = chkRainOl1.IsChecked == true;
        c.EnableRainOutlineRow2 = chkRainOl2.IsChecked == true;
        c.EnableRainOutlineRow3 = chkRainOl3.IsChecked == true;
        c.RainOutlineWidthRow1 = (float)sldRainOlW1.Value;
        c.RainOutlineWidthRow2 = (float)sldRainOlW2.Value;
        c.RainOutlineWidthRow3 = (float)sldRainOlW3.Value;

        c.EnableGhostRain = chkGhostRain.IsChecked == true;
        c.GhostRainStartYRow1 = (float)sldGhostY1.Value;
        c.GhostRainStartYRow2 = (float)sldGhostY2.Value;
        c.GhostRainStartYRow3 = (float)sldGhostY3.Value;
        c.EnableGhostRainShadowRow1 = chkGRSh1.IsChecked == true;
        c.EnableGhostRainShadowRow2 = chkGRSh2.IsChecked == true;
        c.EnableGhostRainShadowRow3 = chkGRSh3.IsChecked == true;
        c.GhostRainShadowOffsetXRow1 = (float)sldGRShX1.Value; c.GhostRainShadowOffsetYRow1 = (float)sldGRShY1.Value;
        c.GhostRainShadowOffsetXRow2 = (float)sldGRShX2.Value; c.GhostRainShadowOffsetYRow2 = (float)sldGRShY2.Value;
        c.GhostRainShadowOffsetXRow3 = (float)sldGRShX3.Value; c.GhostRainShadowOffsetYRow3 = (float)sldGRShY3.Value;
        c.EnableGhostRainOutlineRow1 = chkGROl1.IsChecked == true;
        c.EnableGhostRainOutlineRow2 = chkGROl2.IsChecked == true;
        c.EnableGhostRainOutlineRow3 = chkGROl3.IsChecked == true;
        c.GhostRainOutlineWidthRow1 = (float)sldGROlW1.Value;
        c.GhostRainOutlineWidthRow2 = (float)sldGROlW2.Value;
        c.GhostRainOutlineWidthRow3 = (float)sldGROlW3.Value;

    }
}
