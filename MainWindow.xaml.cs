using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace WindowsPersonalize;
public partial class MainWindow : Window
{
 private readonly DispatcherTimer timer = new(){Interval=TimeSpan.FromSeconds(2)};
 private readonly List<(string Name,string Path,string Icon)> apps = new();
 private DateTime lastCpu=DateTime.UtcNow; private TimeSpan lastCpuTime=TimeSpan.Zero; private bool light=false; private bool effects=true;
 private string wallpaperBackup="";
 public MainWindow(){InitializeComponent(); LoadApps(); timer.Tick+=(_,_)=>UpdateStats(); timer.Start(); UpdateStats(); ClockText.Text=DateTime.Now.ToString("dddd, dd MMMM  •  HH:mm"); KeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Escape)ReturnToWindows(this,new RoutedEventArgs());};}
 private void LoadApps(){AddBuiltin("Explorador","explorer.exe","📁");AddBuiltin("Navegador","https://www.google.com","🌐");AddBuiltin("Bloc de notas","notepad.exe","📝");AddBuiltin("Calculadora","calc.exe","🧮");AddBuiltin("Paint","mspaint.exe","🎨");AddBuiltin("Configuración","ms-settings:","⚙");AddBuiltin("Terminal","wt.exe","⌘");AddBuiltin("Administrador de tareas","taskmgr.exe","▥");RenderApps();}
 private void AddBuiltin(string n,string p,string i)=>apps.Add((n,p,i));
 private void RenderApps(string filter=""){AppGrid.Children.Clear();foreach(var a in apps.Where(a=>a.Name.Contains(filter,StringComparison.OrdinalIgnoreCase))){var b=new Button{Style=(Style)FindResource("TileButton"),Content=new StackPanel{Children={new TextBlock{Text=a.Icon,FontSize=34,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,4,0,8)},new TextBlock{Text=a.Name,HorizontalAlignment=HorizontalAlignment.Center,TextWrapping=TextWrapping.Wrap}}},Tag=a};b.Click+=(_,_)=>Launch((string)((ValueTuple<string,string,string>)b.Tag!).Item2);b.ContextMenu=new ContextMenu();var rename=new MenuItem{Header="Cambiar nombre"};rename.Click+=(_,_)=>{var name=Ask("Nuevo nombre:",a.Name);if(!string.IsNullOrWhiteSpace(name)){int ix=apps.IndexOf(a);apps[ix]=(name,a.Path,a.Icon);RenderApps();}};var icon=new MenuItem{Header="Cambiar icono (emoji)"};icon.Click+=(_,_)=>{var val=Ask("Escribe un emoji o símbolo:",a.Icon);if(!string.IsNullOrWhiteSpace(val)){int ix=apps.IndexOf(a);apps[ix]=(a.Name,a.Path,val);RenderApps();}};var remove=new MenuItem{Header="Quitar del escritorio"};remove.Click+=(_,_)=>{apps.Remove(a);RenderApps();};b.ContextMenu.Items.Add(rename);b.ContextMenu.Items.Add(icon);b.ContextMenu.Items.Add(remove);AppGrid.Children.Add(b);}}
 private string? Ask(string prompt,string initial){var w=new Window{Title="Personalizar",Width=360,Height=150,WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=this,Background=new SolidColorBrush(Color.FromRgb(12,12,32)),Foreground=Brushes.White};var p=new StackPanel{Margin=new Thickness(14)};p.Children.Add(new TextBlock{Text=prompt,Margin=new Thickness(0,0,0,8)});var t=new TextBox{Text=initial,Padding=new Thickness(6)};p.Children.Add(t);var ok=new Button{Content="Aceptar",Margin=new Thickness(0,8,0,0),Padding=new Thickness(8)};ok.Click+=(_,_)=>w.DialogResult=true;p.Children.Add(ok);w.Content=p;return w.ShowDialog()==true?t.Text:null;}
 private void Launch(string path){try{if(path.StartsWith("http"))Process.Start(new ProcessStartInfo(path){UseShellExecute=true});else Process.Start(new ProcessStartInfo(path){UseShellExecute=true});}catch(Exception ex){MessageBox.Show("No se pudo abrir: "+ex.Message,"NEXUS");}}
 private void AddApp(object s,RoutedEventArgs e){var d=new OpenFileDialog{Title="Selecciona una aplicación o acceso directo",Filter="Aplicaciones y accesos directos|*.exe;*.lnk;*.url|Todos los archivos|*.*"};if(d.ShowDialog()==true){apps.Add((Path.GetFileNameWithoutExtension(d.FileName),d.FileName,"✦"));RenderApps();}}
 private void SearchFocus(object s,RoutedEventArgs e){if(SearchBox.Text=="Buscar aplicaciones o ajustes...")SearchBox.Clear();}
 private void SearchChanged(object s,TextChangedEventArgs e){if(AppGrid!=null)RenderApps(SearchBox.Text=="Buscar aplicaciones o ajustes..."?"":SearchBox.Text);}
 private void ChooseWallpaper(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Imágenes|*.png;*.jpg;*.jpeg;*.bmp"};if(d.ShowDialog()==true){try{var uri=new Uri(d.FileName);Background=new ImageBrush(new BitmapImage(uri)){Stretch=Stretch.UniformToFill,Opacity=.30};StatusText.Text="Fondo aplicado a NEXUS";}catch(Exception ex){MessageBox.Show(ex.Message);}}}
 private void ToggleTheme(object s,RoutedEventArgs e){light=!light;Background=new SolidColorBrush(light?Color.FromRgb(23,26,48):Color.FromRgb(5,7,19));StatusText.Text=light?"Tema alternativo":"Tema NEXUS Dark";}
 private void ToggleEffects(object s,RoutedEventArgs e){effects=!effects;Opacity=effects?1:.97;StatusText.Text=effects?"Efectos activados":"Efectos reducidos";}
 private void Personalize(object s,RoutedEventArgs e){ChooseWallpaper(s,e);}
 private void OpenTaskbar(object s,RoutedEventArgs e)=>Launch("ms-settings:taskbar");
 private void OpenSettings(object s,RoutedEventArgs e)=>Launch("ms-settings:");
 private void OpenSystem(object s,RoutedEventArgs e)=>Launch("ms-settings:system");
 private void OpenFiles(object s,RoutedEventArgs e)=>Launch("explorer.exe");
 private void ShowHome(object s,RoutedEventArgs e){SearchBox.Clear();RenderApps();}
 private void GamingMode(object s,RoutedEventArgs e)=>SetMode("MODO JUEGO");
 private void NormalMode(object s,RoutedEventArgs e)=>SetMode("MODO NORMAL");
 private void WorkMode(object s,RoutedEventArgs e)=>SetMode("MODO TRABAJO");
 private void EcoMode(object s,RoutedEventArgs e)=>SetMode("MODO AHORRO");
 private void SetMode(string mode){ModeText.Text=mode;StatusText.Text="Perfil seleccionado";try{if(mode=="MODO JUEGO")Process.Start(new ProcessStartInfo("powercfg","/setactive SCHEME_MIN"){CreateNoWindow=true,UseShellExecute=false});else if(mode=="MODO AHORRO")Process.Start(new ProcessStartInfo("powercfg","/setactive SCHEME_MAX"){CreateNoWindow=true,UseShellExecute=false});else Process.Start(new ProcessStartInfo("powercfg","/setactive SCHEME_BALANCED"){CreateNoWindow=true,UseShellExecute=false});}catch{}}
 private void UpdateStats(){try{var p=Process.GetCurrentProcess();var now=DateTime.UtcNow;var cpu=p.TotalProcessorTime;var pct=(cpu-lastCpuTime).TotalMilliseconds/Math.Max(1,(now-lastCpu).TotalMilliseconds)/Environment.ProcessorCount*100;lastCpu=now;lastCpuTime=cpu;CpuText.Text=$"CPU (app): {Math.Clamp(pct,0,100):0}%";CpuBar.Value=Math.Clamp(pct,0,100);var mem=GC.GetGCMemoryInfo();var total=mem.TotalAvailableMemoryBytes;var used=Process.GetCurrentProcess().WorkingSet64;var ramPct=total>0?used*100.0/total:0;RamText.Text=$"RAM (app): {used/1024.0/1024:0} MB";RamBar.Value=Math.Clamp(ramPct,0,100);var d=new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);DiskText.Text=$"DISCO C: {d.AvailableFreeSpace/1024.0/1024/1024:0} GB libres";}catch{}}
 private void CleanTemp(object s,RoutedEventArgs e){if(MessageBox.Show("Se eliminarán archivos temporales del usuario que tengan más de 3 días. ¿Continuar?","Limpieza",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;int n=0;try{foreach(var f in Directory.GetFiles(Path.GetTempPath()))try{if(File.GetLastWriteTimeUtc(f)<DateTime.UtcNow.AddDays(-3)){File.Delete(f);n++;}}catch{}StatusText.Text=$"Limpieza completada: {n} archivos";}catch(Exception ex){MessageBox.Show(ex.Message);}}
 private void StartupToggle(object s,RoutedEventArgs e){try{string exe=Environment.ProcessPath!;using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true)!;if(key.GetValue("WindowsPersonalize")!=null){key.DeleteValue("WindowsPersonalize");StatusText.Text="Inicio automático desactivado";}else{key.SetValue("WindowsPersonalize",'"'+exe+'"');StatusText.Text="Inicio automático activado";}}catch(Exception ex){MessageBox.Show("No se pudo cambiar el inicio: "+ex.Message);}}
 private void Restore(object s,RoutedEventArgs e){if(MessageBox.Show("¿Restaurar el tema oscuro de NEXUS y el modo de energía equilibrado?","Restaurar",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;light=false;effects=true;Background=new SolidColorBrush(Color.FromRgb(5,7,19));SetMode("MODO NORMAL");StatusText.Text="Preferencias visuales restauradas";}
 private void Updates(object s,RoutedEventArgs e)=>Launch("https://github.com/misaelalejandro356l-cloud/WINDOWS-PERSONALIZE/releases");
 private void ReturnToWindows(object s,RoutedEventArgs e){timer.Stop();Close();}
}