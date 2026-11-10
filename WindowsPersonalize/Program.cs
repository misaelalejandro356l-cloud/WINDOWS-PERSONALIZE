using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowsPersonalize;

internal static class Program
{
    [STAThread] static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new NexusForm());
    }
}

public sealed class Tile
{
    public string Name { get; set; } = "Aplicación";
    public string Target { get; set; } = "";
    public string? IconFile { get; set; }
}
public sealed class Settings
{
    public string? Wallpaper { get; set; }
    public string Theme { get; set; } = "NEXUS Dark";
    public string Accent { get; set; } = "#A52BFF";
    public string Profile { get; set; } = "Normal";
    public bool StartWithWindows { get; set; }
    public List<Tile> Tiles { get; set; } = new();
}
public sealed class NexusForm : Form
{
    readonly string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsPersonalize");
    Settings cfg = new();
    FlowLayoutPanel apps = new();
    Label cpu = new(), ram = new(), clock = new(), themeInfo = new();
    System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    readonly Color purple = Color.FromArgb(174, 66, 255);
    readonly Color glass = Color.FromArgb(225, 9, 8, 29);
    DateTime lastCpuAt = DateTime.UtcNow;
    TimeSpan lastCpu = TimeSpan.Zero;

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    struct MEMORYSTATUSEX { public uint dwLength,dwMemoryLoad; public ulong ullTotalPhys,ullAvailPhys,ullTotalPageFile,ullAvailPageFile,ullTotalVirtual,ullAvailVirtual,ullAvailExtendedVirtual; }
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public NexusForm()
    {
        Directory.CreateDirectory(data);
        LoadSettings();
        Text="NEXUS Desktop"; FormBorderStyle=FormBorderStyle.None; WindowState=FormWindowState.Maximized;
        MinimumSize=new Size(1100,700); BackColor=Color.FromArgb(4,4,20); ForeColor=Color.White;
        KeyPreview=true; KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape) ReturnToWindows();};
        BuildUI(); ApplyWallpaper(); timer.Tick+=(_,_)=>UpdateStats(); timer.Start(); UpdateStats();
        FormClosing+=(_,_)=>timer.Stop();
    }
    string ConfigPath=>Path.Combine(data,"settings.json");
    void LoadSettings()
    {
        try { if(File.Exists(ConfigPath)) cfg=JsonSerializer.Deserialize<Settings>(File.ReadAllText(ConfigPath))??new(); } catch { cfg=new(); }
        if(cfg.Tiles.Count==0) cfg.Tiles=DefaultTiles();
        Save();
    }
    static List<Tile> DefaultTiles()=>new(){
        new(){Name="Brave",Target="brave.exe"},new(){Name="Play Store",Target="ms-windows-store:"},
        new(){Name="Aplicaciones NEXUS",Target="ms-settings:appsfeatures"},new(){Name="Ajustes",Target="ms-settings:"},
        new(){Name="Galería",Target="ms-photos:"},new(){Name="Administrador",Target="explorer.exe"},
        new(){Name="Descargas",Target="shell:downloads"},new(){Name="Calculadora",Target="calc.exe"},
        new(){Name="Calendario",Target="outlookcal:"},new(){Name="Cámara",Target="microsoft.windows.camera:"},
        new(){Name="Reloj",Target="ms-clock:"},new(){Name="Notas",Target="notepad.exe"},
        new(){Name="Música",Target="mswindowsmusic:"},new(){Name="Videos",Target="wmplayer.exe"},
        new(){Name="Contactos",Target="ms-people:"},new(){Name="Mensajes",Target="ms-settings:messaging"},
        new(){Name="Correo",Target="mailto:"},new(){Name="Navegador",Target="https://www.google.com"},
        new(){Name="Terminal",Target="wt.exe"},new(){Name="Ayuda",Target="ms-contact-support:"}
    };
    void Save(){try{File.WriteAllText(ConfigPath,JsonSerializer.Serialize(cfg,new JsonSerializerOptions{WriteIndented=true}));}catch{}}

    void BuildUI()
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,Padding=new Padding(16),BackColor=Color.Transparent};
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,220)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,285));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,82)); Controls.Add(root);
        var top=Panel(glass); var brand=Lbl("◢  N E X U S",18,12,200,30,16,Color.DeepSkyBlue,true); top.Controls.Add(brand);
        clock=Lbl("",-120,10,100,28,12,Color.White); clock.Anchor=AnchorStyles.Top|AnchorStyles.Right; top.Controls.Add(clock);
        var back=Btn("↩  VOLVER A WINDOWS",0,6,205,34,Color.FromArgb(45,35,85)); back.Anchor=AnchorStyles.Top|AnchorStyles.Right; back.Click+=(_,_)=>ReturnToWindows(); top.Controls.Add(back);
        back.Location=new Point(Math.Max(210,Width-245),6); top.Resize+=(_,_)=>back.Location=new Point(top.Width-215,6);
        root.Controls.Add(top,0,0); root.SetColumnSpan(top,3);

        var side=Panel(glass); side.Padding=new Padding(10);
        side.Controls.Add(Lbl("NEXUS",12,20,190,42,28,purple,true)); side.Controls.Add(Lbl("WINDOWS DESKTOP",14,60,185,24,9,Color.LightSteelBlue));
        string[] nav={"⌂   Inicio","▦   Aplicaciones","🎮   Juegos","⚙   Sistema","✎   Personalización","☷   Ajustes","▰   Archivos"};
        int y=110; foreach(string n in nav){var b=Btn(n,8,y,194,43,Color.FromArgb(40,23,69)); b.TextAlign=ContentAlignment.MiddleLeft; b.Padding=new Padding(12,0,0,0); b.Click+=(_,_)=>Nav(n); side.Controls.Add(b);y+=49;}
        side.Controls.Add(Lbl("ACCESO RÁPIDO",14,y+8,180,22,9,Color.LightSteelBlue));
        var add=Btn("＋  Agregar aplicación",8,y+38,194,42,Color.FromArgb(75,25,120)); add.Click+=(_,_)=>AddTile(); side.Controls.Add(add);
        root.Controls.Add(side,0,1);

        var middle=Panel(Color.Transparent); middle.Padding=new Padding(18,12,18,8); root.Controls.Add(middle,1,1);
        var search=Lbl("⌕   Buscar aplicaciones y ajustes",4,5,Math.Max(350,middle.Width-210),44,12,Color.Gainsboro); search.BackColor=Color.FromArgb(170,16,9,37); search.BorderStyle=BorderStyle.FixedSingle; search.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right; middle.Controls.Add(search);
        var customize=Btn("PERSONALIZAR",0,8,150,38,Color.FromArgb(75,25,125)); customize.Anchor=AnchorStyles.Top|AnchorStyles.Right; customize.Click+=(_,_)=>Personalize(); middle.Controls.Add(customize);
        middle.Resize+=(_,_)=>{search.Width=middle.Width-205; customize.Location=new Point(middle.Width-165,8);apps.Size=new Size(middle.Width-30,middle.Height-85);};
        apps=new FlowLayoutPanel{Location=new Point(0,68),Size=new Size(middle.Width-30,middle.Height-85),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,AutoScroll=true,WrapContents=true,BackColor=Color.Transparent,Padding=new Padding(4)};
        middle.Controls.Add(apps); RenderTiles();
        var hero=Lbl("N",0,0,120,80,58,purple,true); hero.Anchor=AnchorStyles.Bottom; hero.TextAlign=ContentAlignment.MiddleCenter; // small central NEXUS mark
        middle.Controls.Add(hero); hero.Location=new Point(Math.Max(0,(middle.Width-120)/2),middle.Height-95); middle.Resize+=(_,_)=>hero.Location=new Point(Math.Max(0,(middle.Width-120)/2),middle.Height-95);

        var right=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.Transparent,Padding=new Padding(5,18,5,5)};
        right.Controls.Add(Card("RENDIMIENTO",cpu,ram));
        right.Controls.Add(Card("SISTEMA",Lbl("Windows 10 / 11",0,0,230,24,11,Color.Gainsboro),Lbl("NEXUS Desktop · x64",0,0,230,24,11,Color.Gainsboro)));
        themeInfo=Lbl("Tema: "+cfg.Theme,0,0,230,24,11,Color.Gainsboro);
        right.Controls.Add(Card("PERSONALIZACIÓN",themeInfo,Lbl("Iconos: editables por aplicación",0,0,230,24,10,Color.Gainsboro)));
        var game=Btn("🎮  MODO JUEGO",10,5,240,42,Color.FromArgb(80,20,130)); game.Click+=(_,_)=>SetProfile("Gaming"); right.Controls.Add(game);
        var normal=Btn("▣  MODO NORMAL",10,5,240,42,Color.FromArgb(20,55,130)); normal.Click+=(_,_)=>SetProfile("Normal"); right.Controls.Add(normal);
        var power=Btn("⏻  APAGAR",10,5,240,42,Color.FromArgb(55,22,50)); power.Click+=(_,_)=>ReturnToWindows(); right.Controls.Add(power);
        root.Controls.Add(right,2,1);

        var dock=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,AutoScroll=true,BackColor=Color.FromArgb(190,8,7,28),Padding=new Padding(12,12,12,4)};
        AddDock(dock,"📁 Archivos",()=>Open("explorer.exe")); AddDock(dock,"🦁 Brave",()=>Open("brave.exe")); AddDock(dock,"🛍 Store",()=>Open("ms-windows-store:")); AddDock(dock,"⚙ Ajustes",()=>Open("ms-settings:")); AddDock(dock,"＋ Aplicación",AddTile); AddDock(dock,"↩ Volver a Windows",ReturnToWindows);
        root.Controls.Add(dock,1,2); root.SetColumnSpan(dock,2);
    }
    Panel Panel(Color c)=>new(){Dock=DockStyle.Fill,BackColor=c};
    Label Lbl(string t,int x,int y,int w,int h,int s,Color c,bool bold=false)=>new(){Text=t,Location=new Point(x,y),Size=new Size(w,h),ForeColor=c,Font=new Font("Segoe UI",s,bold?FontStyle.Bold:FontStyle.Regular),BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleLeft};
    Button Btn(string t,int x,int y,int w,int h,Color c){var b=new Button{Text=t,Location=new Point(x,y),Size=new Size(w,h),BackColor=c,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",9),Cursor=Cursors.Hand};b.FlatAppearance.BorderColor=Color.FromArgb(95,95,165);b.FlatAppearance.BorderSize=1;return b;}
    Panel Card(string title,params Control[] controls){var p=new Panel{Width=255,Height=125,BackColor=glass,Padding=new Padding(10),Margin=new Padding(0,0,0,12)};p.Controls.Add(Lbl(title,8,5,230,24,10,Color.FromArgb(230,160,255),true));int y=38;foreach(var c in controls){c.Location=new Point(8,y);p.Controls.Add(c);y+=28;}return p;}
    void AddDock(FlowLayoutPanel p,string text,Action a){var b=Btn(text,0,0,125,48,Color.FromArgb(38,28,76));b.Click+=(_,_)=>a();p.Controls.Add(b);}
    void Nav(string n){if(n.Contains("Personalización"))Personalize();else if(n.Contains("Archivos"))Open("explorer.exe");else if(n.Contains("Sistema")||n.Contains("Ajustes"))Open("ms-settings:");else if(n.Contains("Juegos"))Open("ms-settings:gaming-gamebar");else if(n.Contains("Aplicaciones"))AddTile();}
    void RenderTiles()
    {
        apps.SuspendLayout();apps.Controls.Clear();
        foreach(var tile in cfg.Tiles.ToArray()){
            var card=new Panel{Width=118,Height=126,BackColor=Color.FromArgb(115,15,11,40),Margin=new Padding(7)};
            var icon=new Button{Location=new Point(23,5),Size=new Size(72,72),Text=Emoji(tile.Name),Font=new Font("Segoe UI Emoji",25),BackColor=Color.FromArgb(35,32,83),ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand};
            icon.FlatAppearance.BorderColor=Color.FromArgb(75,100,180);
            if(!string.IsNullOrEmpty(tile.IconFile)&&File.Exists(tile.IconFile)){try{using var im=Image.FromFile(tile.IconFile);icon.Image=new Bitmap(im,new Size(58,58));icon.Text="";}catch{}}
            icon.Click+=(_,_)=>Open(tile.Target);card.Controls.Add(icon);
            var label=Lbl(tile.Name,2,80,114,36,10,Color.White);label.TextAlign=ContentAlignment.TopCenter;card.Controls.Add(label);
            var menu=new ContextMenuStrip();menu.Items.Add("Abrir",null,(_,_)=>Open(tile.Target));menu.Items.Add("Cambiar icono…",null,(_,_)=>ChangeIcon(tile));menu.Items.Add("Cambiar nombre…",null,(_,_)=>Rename(tile));menu.Items.Add("Quitar",null,(_,_)=>{cfg.Tiles.Remove(tile);Save();RenderTiles();});icon.ContextMenuStrip=menu;label.ContextMenuStrip=menu;card.ContextMenuStrip=menu;apps.Controls.Add(card);
        }apps.ResumeLayout();
    }
    string Emoji(string n){string s=n.ToLowerInvariant();if(s.Contains("brave"))return "🦁";if(s.Contains("store"))return "🛍";if(s.Contains("ajuste"))return "⚙";if(s.Contains("galer"))return "🌄";if(s.Contains("archiv")||s.Contains("descarg"))return "📁";if(s.Contains("calcul"))return "🧮";if(s.Contains("calend"))return "📅";if(s.Contains("cámara"))return "📷";if(s.Contains("reloj"))return "🕒";if(s.Contains("nota"))return "📝";if(s.Contains("música"))return "🎵";if(s.Contains("video"))return "▶";if(s.Contains("terminal"))return "⌨";return "N";}
    void AddTile(){using var d=new OpenFileDialog{Title="Añadir aplicación o acceso directo",Filter="Aplicaciones y accesos|*.exe;*.lnk;*.url|Todos los archivos|*.*"};if(d.ShowDialog()!=DialogResult.OK)return;cfg.Tiles.Add(new Tile{Name=Path.GetFileNameWithoutExtension(d.FileName),Target=d.FileName});Save();RenderTiles();}
    void ChangeIcon(Tile t){using var d=new OpenFileDialog{Title="Selecciona icono",Filter="Iconos e imágenes|*.ico;*.png;*.jpg;*.jpeg;*.bmp"};if(d.ShowDialog()!=DialogResult.OK)return;try{string ext=Path.GetExtension(d.FileName);string dest=Path.Combine(data,"icon_"+Guid.NewGuid().ToString("N")+ext);File.Copy(d.FileName,dest);t.IconFile=dest;Save();RenderTiles();}catch(Exception ex){MessageBox.Show(ex.Message);}}
    void Rename(Tile t){using var f=new Form{Text="Cambiar nombre",Size=new Size(340,145),StartPosition=FormStartPosition.CenterParent};var box=new TextBox{Text=t.Name,Location=new Point(15,15),Width=290};var ok=new Button{Text="Guardar",DialogResult=DialogResult.OK,Location=new Point(210,55)};f.Controls.Add(box);f.Controls.Add(ok);f.AcceptButton=ok;if(f.ShowDialog(this)==DialogResult.OK&&!string.IsNullOrWhiteSpace(box.Text)){t.Name=box.Text.Trim();Save();RenderTiles();}}
    void Open(string target){try{Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}catch{MessageBox.Show("No se pudo abrir. Comprueba que la aplicación esté instalada o vuelve a agregar su acceso directo.","NEXUS");}}
    void Personalize(){using var f=new Form{Text="Personalización NEXUS",Size=new Size(440,330),StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(15,10,35),ForeColor=Color.White};var t=Lbl("Tema",18,15,100,25,11,Color.White);f.Controls.Add(t);var combo=new ComboBox{Location=new Point(18,45),Width=380,DropDownStyle=ComboBoxStyle.DropDownList};combo.Items.AddRange(new object[]{"NEXUS Dark","NEXUS Neon","Azul Cósmico","Violeta"});combo.SelectedItem=cfg.Theme;if(combo.SelectedIndex<0)combo.SelectedIndex=0;f.Controls.Add(combo);var wall=Btn("Elegir fondo de pantalla",18,90,380,40,Color.FromArgb(70,25,115));wall.Click+=(_,_)=>{f.Hide();ChooseWallpaper();f.Show();};f.Controls.Add(wall);var color=Btn("Elegir color de acento",18,140,380,40,Color.FromArgb(70,25,115));color.Click+=(_,_)=>{using var d=new ColorDialog();if(d.ShowDialog()==DialogResult.OK)cfg.Accent=ColorTranslator.ToHtml(d.Color);};f.Controls.Add(color);var reset=Btn("Restablecer iconos y aplicaciones",18,190,380,40,Color.FromArgb(45,35,80));reset.Click+=(_,_)=>{if(MessageBox.Show("¿Restablecer el lanzador?","NEXUS",MessageBoxButtons.YesNo)==DialogResult.Yes){cfg.Tiles=DefaultTiles();Save();RenderTiles();}};f.Controls.Add(reset);var save=Btn("Guardar",268,245,130,35,Color.FromArgb(95,25,155));save.Click+=(_,_)=>{cfg.Theme=combo.SelectedItem?.ToString()??"NEXUS Dark";Save();themeInfo.Text="Tema: "+cfg.Theme;f.Close();};f.Controls.Add(save);f.ShowDialog(this);}
    void ChooseWallpaper(){using var d=new OpenFileDialog{Title="Elegir fondo",Filter="Imágenes|*.jpg;*.jpeg;*.png;*.bmp"};if(d.ShowDialog()!=DialogResult.OK)return;string dest=Path.Combine(data,"wallpaper"+Path.GetExtension(d.FileName));File.Copy(d.FileName,dest,true);cfg.Wallpaper=dest;Save();ApplyWallpaper();}
    void ApplyWallpaper(){if(!string.IsNullOrWhiteSpace(cfg.Wallpaper)&&File.Exists(cfg.Wallpaper)){try{BackgroundImage=Image.FromFile(cfg.Wallpaper);BackgroundImageLayout=ImageLayout.Stretch;}catch{}}}
    void SetProfile(string p){cfg.Profile=p;try{var args=p=="Gaming"?"/setactive SCHEME_MIN":"/setactive SCHEME_BALANCED";Process.Start(new ProcessStartInfo("powercfg.exe",args){UseShellExecute=false,CreateNoWindow=true})?.WaitForExit(3000);}catch{}Save();MessageBox.Show("Perfil "+p+" seleccionado. Windows puede no admitir todos los planes de energía.","NEXUS");}
    void UpdateStats(){clock.Text=DateTime.Now.ToString("h:mm tt");try{var m=new MEMORYSTATUSEX{dwLength=(uint)Marshal.SizeOf<MEMORYSTATUSEX>()};GlobalMemoryStatusEx(ref m);ram.Text=$"RAM usada: {m.dwMemoryLoad}%  ·  {(m.ullTotalPhys-m.ullAvailPhys)/1024/1024/1024.0:0.0} / {m.ullTotalPhys/1024/1024/1024.0:0.0} GB";var now=DateTime.UtcNow;var total=Process.GetProcesses().Sum(p=>{try{return p.TotalProcessorTime.TotalMilliseconds;}catch{return 0;}});var span=(now-lastCpuAt).TotalMilliseconds*Math.Max(1,Environment.ProcessorCount);double pct=lastCpuAt==DateTime.MinValue?0:Math.Clamp((total-lastCpu.TotalMilliseconds)/Math.Max(1,span)*100,0,100);cpu.Text=$"CPU: {pct:0}%  ·  {Process.GetProcesses().Length} procesos";lastCpu=TimeSpan.FromMilliseconds(total);lastCpuAt=now;}catch{cpu.Text="CPU: —";ram.Text="RAM: —";}}
    void ReturnToWindows(){if(MessageBox.Show("NEXUS se cerrará y volverás al escritorio normal de Windows. No se desinstala ni modifica Windows. ¿Continuar?","Volver a Windows",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){Save();Close();}}
}