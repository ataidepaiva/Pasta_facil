using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Windows.Forms;

namespace PastaFacil;

internal static class Program
{
    [STAThread]
    static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new MainForm()); }
}

public sealed class MainForm : Form
{
    readonly Color blue = Color.FromArgb(44, 75, 180), background = Color.FromArgb(244, 247, 252);
    readonly TextBox input = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill };
    readonly CheckBox header = new() { Text = "A primeira linha contém os títulos das colunas", Checked = true, AutoSize = true };
    readonly CheckedListBox columns = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    readonly TextBox root = new() { Dock = DockStyle.Fill, ReadOnly = true };
    readonly TextBox preview = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, ReadOnly = true, Dock = DockStyle.Fill };
    readonly ListBox files = new() { Dock = DockStyle.Fill, SelectionMode = SelectionMode.MultiExtended, HorizontalScrollbar = true };
    readonly ListBox destinations = new() { Dock = DockStyle.Fill, SelectionMode = SelectionMode.MultiExtended, HorizontalScrollbar = true };
    readonly TextBox subfolder = new() { Dock = DockStyle.Fill, PlaceholderText = "Opcional: nome de uma subpasta já existente" };
    readonly ComboBox conflict = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly TextBox log = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, ReadOnly = true, Dock = DockStyle.Fill };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(12, 6, 0, 0), Text = "Pronto para começar.", BackColor = Color.White };
    List<string[]> rows = new();
    List<int> order = new();
    string[] names = Array.Empty<string>();
    List<string> planned = new();
    bool dirty = true;

    public MainForm()
    {
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Text = "PastaFácil | Pastas e arquivos em massa"; Size = new Size(1100, 820); MinimumSize = new Size(800, 650);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10); BackColor = background;
        var top = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = blue, Padding = new Padding(20, 10, 0, 0) };
        top.Controls.Add(new Label { Text = "PastaFácil", ForeColor = Color.White, Font = new Font("Segoe UI", 20, FontStyle.Bold), AutoSize = true, Location = new Point(20, 8) });
        top.Controls.Add(new Label { Text = "Organize pastas e distribua arquivos em poucos passos", ForeColor = Color.White, AutoSize = true, Location = new Point(24, 51) });
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(18, 8) };
        tabs.TabPages.Add(BuildCreate()); tabs.TabPages.Add(BuildCopy()); tabs.TabPages.Add(BuildReport());
        var footer = new Label { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.White, ForeColor = Color.FromArgb(90, 100, 115), Font = new Font("Segoe UI", 9), Text = $"Criado por Ataide de Paula Paiva - {DateTime.Now.Year}. Todos os Direitos Reservados" };
        Controls.Add(tabs); Controls.Add(status); Controls.Add(footer); Controls.Add(top);
        input.TextChanged += (_, _) => { dirty = true; ClearPreview(); };
        header.CheckedChanged += (_, _) => { dirty = true; ClearPreview(); };
        root.TextChanged += (_, _) => ClearPreview();
        columns.ItemCheck += (_, _) => { ClearPreview(); };
    }
    Button Btn(string text, EventHandler action, bool primary = false) {
        var b = new Button { Text = text, AutoSize = true, Height = 34, Padding = new Padding(8, 2, 8, 2), FlatStyle = FlatStyle.Flat, BackColor = primary ? blue : Color.White, ForeColor = primary ? Color.White : blue, Margin = new Padding(3) };
        b.FlatAppearance.BorderColor = blue; b.Click += action; return b;
    }
    static Label Caption(string s) => new() { Text = s, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(3, 8, 3, 4) };
    static FlowLayoutPanel Bar() => new() { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
    static TableLayoutPanel Grid() => new() { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 0, BackColor = Color.FromArgb(244,247,252) };
    static void Row(TableLayoutPanel p, Control c, SizeType type, float size) { int i = p.RowCount++; p.RowStyles.Add(new RowStyle(type, size)); p.Controls.Add(c, 0, i); }
    TabPage BuildCreate() {
        var page = new TabPage("Criar pastas"); var p = Grid(); page.Controls.Add(p);
        Row(p, Caption("1. Cole os dados da planilha ou uma lista de nomes"), SizeType.AutoSize, 0);
        Row(p, input, SizeType.Percent, 38);
        var b = Bar(); b.Controls.Add(header); b.Controls.Add(Btn("Ler colunas", (_,_) => ReadColumns(), true)); Row(p,b,SizeType.AutoSize,0);
        Row(p, Caption("2. Marque as colunas e defina a ordem das pastas"), SizeType.AutoSize,0);
        var cols = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 }; cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); cols.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));
        cols.Controls.Add(columns,0,0); var cb = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        cb.Controls.Add(Btn("↑ Subir", (_,_) => MoveColumn(-1))); cb.Controls.Add(Btn("↓ Descer", (_,_) => MoveColumn(1))); cols.Controls.Add(cb,1,0); Row(p,cols,SizeType.Percent,25);
        Row(p,Caption("3. Escolha a pasta principal"),SizeType.AutoSize,0);
        var dest = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 }; dest.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); dest.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); dest.Controls.Add(root,0,0); dest.Controls.Add(Btn("Escolher…", (_,_) => PickRoot()),1,0); Row(p,dest,SizeType.AutoSize,0);
        var actions = Bar(); actions.Controls.Add(Btn("Atualizar prévia",(_,_) => Preview(),true)); actions.Controls.Add(Btn("Criar pastas",(_,_) => CreateFolders(),true)); Row(p,actions,SizeType.AutoSize,0);
        Row(p,Caption("4. Confira os caminhos antes de criar"),SizeType.AutoSize,0); Row(p,preview,SizeType.Percent,37); return page;
    }
    TabPage BuildCopy() {
        var page = new TabPage("Copiar arquivos"); var p = Grid(); page.Controls.Add(p);
        Row(p,Caption("1. Selecione os arquivos que serão distribuídos"),SizeType.AutoSize,0); Row(p,files,SizeType.Percent,33);
        var fb=Bar(); fb.Controls.Add(Btn("Adicionar arquivos",(_,_)=>AddFiles())); fb.Controls.Add(Btn("Remover selecionados",(_,_)=>RemoveSelected(files))); Row(p,fb,SizeType.AutoSize,0);
        Row(p,Caption("2. Selecione as pastas que receberão os arquivos"),SizeType.AutoSize,0); Row(p,destinations,SizeType.Percent,33);
        var db=Bar(); db.Controls.Add(Btn("Adicionar pasta",(_,_)=>AddDestination())); db.Controls.Add(Btn("Usar pastas da prévia",(_,_)=>UsePreview())); db.Controls.Add(Btn("Remover selecionadas",(_,_)=>RemoveSelected(destinations))); Row(p,db,SizeType.AutoSize,0);
        Row(p,Caption("Subpasta em cada destino (opcional)"),SizeType.AutoSize,0); Row(p,subfolder,SizeType.AutoSize,0);
        Row(p,Caption("Se um arquivo com o mesmo nome já existir"),SizeType.AutoSize,0); conflict.Items.AddRange(new object[]{"Ignorar", "Manter os dois", "Substituir"}); conflict.SelectedIndex=0; Row(p,conflict,SizeType.AutoSize,0);
        var act=Bar(); act.Controls.Add(Btn("Ver prévia da cópia",(_,_)=>CopyFiles(false))); act.Controls.Add(Btn("Copiar arquivos",(_,_)=>CopyFiles(true),true)); Row(p,act,SizeType.AutoSize,0);
        Row(p,new Label { Text="As subpastas indicadas precisam existir. A prévia não altera arquivos.", AutoSize=true },SizeType.AutoSize,0); return page;
    }
    TabPage BuildReport() { var page=new TabPage("Relatório"); var p=Grid(); page.Controls.Add(p); Row(p,Caption("Atividade desta sessão"),SizeType.AutoSize,0); Row(p,log,SizeType.Percent,100); var b=Bar(); b.Controls.Add(Btn("Salvar relatório",(_,_)=>SaveLog())); b.Controls.Add(Btn("Limpar relatório",(_,_)=>log.Clear())); Row(p,b,SizeType.AutoSize,0); return page; }
    void ClearPreview() { planned.Clear(); preview.Clear(); }
    void Note(string s) { log.AppendText($"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {s}{Environment.NewLine}"); status.Text=s; }
    void Alert(string s) => MessageBox.Show(this,s,"PastaFácil",MessageBoxButtons.OK,MessageBoxIcon.Information);
    void PickRoot() { using var d=new FolderBrowserDialog { Description="Escolha a pasta principal" }; if(d.ShowDialog()==DialogResult.OK) root.Text=d.SelectedPath; }
    // Excel places tab-separated data on the clipboard; quoted cells may contain tabs and newlines.
    static List<string[]> ParseTsv(string text) {
        var result=new List<string[]>(); var record=new List<string>(); var cell=new StringBuilder(); bool quote=false;
        for(int i=0;i<text.Length;i++) { char c=text[i]; if(quote) { if(c=='"') { if(i+1<text.Length && text[i+1]=='"') { cell.Append('"'); i++; } else quote=false; } else cell.Append(c); }
            else if(c=='"' && cell.Length==0) quote=true; else if(c=='\t') {record.Add(cell.ToString());cell.Clear();} else if(c=='\r'||c=='\n') {record.Add(cell.ToString());cell.Clear(); if(record.Any(x=>!string.IsNullOrWhiteSpace(x))) result.Add(record.ToArray()); record=new(); if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n') i++;} else cell.Append(c); }
        if(quote) throw new Exception("Há uma célula com aspas sem fechamento."); record.Add(cell.ToString()); if(record.Any(x=>!string.IsNullOrWhiteSpace(x))) result.Add(record.ToArray()); return result;
    }
    bool ReadColumns() { try { var all=ParseTsv(input.Text); if(all.Count==0 || (header.Checked && all.Count<2)) throw new Exception("Cole pelo menos uma linha de dados."); int width=all.Max(x=>x.Length); names=Enumerable.Range(0,width).Select(i=>header.Checked && i<all[0].Length && !string.IsNullOrWhiteSpace(all[0][i]) ? all[0][i].Trim() : $"Coluna {i+1}").ToArray(); rows=all.Skip(header.Checked?1:0).Select(r=>Enumerable.Range(0,width).Select(i=>i<r.Length?r[i]:"").ToArray()).ToList(); order=Enumerable.Range(0,width).ToList(); columns.Items.Clear(); foreach(int i in order) columns.Items.Add($"{i+1}. {names[i]}",true); dirty=false; ClearPreview(); status.Text=$"{rows.Count} linhas e {width} colunas identificadas."; return true; } catch(Exception e) { Alert(e.Message); return false; } }
    void MoveColumn(int delta) { int i=columns.SelectedIndex,j=i+delta; if(i<0||j<0||j>=order.Count)return; var state=Enumerable.Range(0,columns.Items.Count).Select(columns.GetItemChecked).ToList(); (order[i],order[j])=(order[j],order[i]); (state[i],state[j])=(state[j],state[i]); columns.Items.Clear(); for(int k=0;k<order.Count;k++) columns.Items.Add($"{order[k]+1}. {names[order[k]]}",state[k]); columns.SelectedIndex=j; ClearPreview(); }
    static string Name(string raw) { string n=raw.Trim(); if(n.Length==0||n=="."||n==".."||n.EndsWith('.')||n.IndexOfAny(Path.GetInvalidFileNameChars())>=0||Regex.IsMatch(n,@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?$",RegexOptions.IgnoreCase)) throw new Exception($"Nome de pasta inválido: '{raw}'"); return n; }
    bool Preview() { try { if(dirty && !ReadColumns())return false; if(!Directory.Exists(root.Text))throw new Exception("Escolha uma pasta principal existente."); var selected=Enumerable.Range(0,order.Count).Where(i=>columns.GetItemChecked(i)).Select(i=>order[i]).ToArray(); if(selected.Length==0)throw new Exception("Marque ao menos uma coluna."); var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase); for(int r=0;r<rows.Count;r++) { string p=root.Text; try { foreach(int i in selected) { p=Path.Combine(p,Name(rows[r][i])); paths.Add(p); } } catch(Exception e) { throw new Exception($"Linha {r+(header.Checked?2:1)}: {e.Message}"); } } planned=paths.OrderBy(p=>p.Length).ThenBy(p=>p,StringComparer.OrdinalIgnoreCase).ToList(); preview.Lines=planned.Select(p=>(Directory.Exists(p)?"[Já existe] ":"[Criar] ")+p).ToArray(); status.Text=$"{planned.Count} caminhos; {planned.Count(Directory.Exists)} já existem."; return true; } catch(Exception e) { ClearPreview(); Alert(e.Message); return false; } }
    void CreateFolders() { if(!Preview())return; if(MessageBox.Show(this,$"Criar {planned.Count} caminhos em:\n{root.Text}?\n\nPastas existentes serão mantidas.","Confirmar criação",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return; int created=0,existing=0,errors=0; foreach(string p in planned) try { if(Directory.Exists(p))existing++; else {Directory.CreateDirectory(p);created++;} } catch(Exception e) {errors++;Note($"ERRO ao criar {p}: {e.Message}");} Note($"Pastas criadas: {created}; existentes: {existing}; erros: {errors}."); Alert(status.Text+"\nConsulte a aba Relatório para os detalhes."); }
    void AddFiles() { using var d=new OpenFileDialog { Multiselect=true, Title="Escolha os arquivos" }; if(d.ShowDialog()==DialogResult.OK) foreach(string f in d.FileNames) if(!files.Items.Cast<string>().Contains(f,StringComparer.OrdinalIgnoreCase))files.Items.Add(f); }
    void AddDestination() { using var d=new FolderBrowserDialog { Description="Escolha uma pasta de destino" }; if(d.ShowDialog()==DialogResult.OK) AddDest(d.SelectedPath); }
    void AddDest(string p) { if(!destinations.Items.Cast<string>().Contains(p,StringComparer.OrdinalIgnoreCase)) destinations.Items.Add(p); }
    void UsePreview() { if(planned.Count==0) {Alert("Gere uma prévia na aba Criar pastas primeiro.");return;} foreach(string p in planned.Where(Directory.Exists))AddDest(p); Alert("Pastas existentes da prévia adicionadas. A cópia usa todas as pastas da lista."); }
    static void RemoveSelected(ListBox list) { for(int i=list.SelectedIndices.Count-1;i>=0;i--)list.Items.RemoveAt(list.SelectedIndices[i]); }
    void CopyFiles(bool execute) { try { if(files.Items.Count==0||destinations.Items.Count==0)throw new Exception("Adicione pelo menos um arquivo e uma pasta de destino."); string sub=string.IsNullOrWhiteSpace(subfolder.Text)?"":Name(subfolder.Text); var targets=destinations.Items.Cast<string>().Select(p=>string.IsNullOrEmpty(sub)?p:Path.Combine(p,sub)).ToList(); foreach(string t in targets) if(!Directory.Exists(t))throw new Exception($"Pasta não encontrada: {t}\nCrie-a antes de copiar."); foreach(string f in files.Items) if(!File.Exists(f))throw new Exception($"Arquivo não encontrado: {f}"); var lines=new List<string>(); foreach(string t in targets)foreach(string f in files.Items) { string dest=Path.Combine(t,Path.GetFileName(f)); lines.Add($"{f} → {dest}"+(File.Exists(dest)?$" [já existe: {conflict.Text}]":"")); } if(!execute) { previewCopy(lines);return; } if(MessageBox.Show(this,$"Copiar {files.Items.Count} arquivo(s) para {targets.Count} pasta(s)?\n\nPolítica para nomes repetidos: {conflict.Text}.","Confirmar cópia",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return; int copied=0,skipped=0,err=0; foreach(string t in targets)foreach(string f in files.Items) try {string dest=Path.Combine(t,Path.GetFileName(f)); if(string.Equals(Path.GetFullPath(f),Path.GetFullPath(dest),StringComparison.OrdinalIgnoreCase)) {skipped++;Note($"IGNORADO (origem e destino iguais): {f}");continue;} if(File.Exists(dest)&&conflict.SelectedIndex==0){skipped++;Note($"IGNORADO: {dest}");continue;} if(conflict.SelectedIndex==1) {string stem=Path.GetFileNameWithoutExtension(dest), ext=Path.GetExtension(dest); int n=1; while(File.Exists(dest))dest=Path.Combine(t,$"{stem} ({n++}){ext}");} File.Copy(f,dest,conflict.SelectedIndex==2); copied++;Note($"COPIADO: {f} → {dest}"); } catch(Exception e) {err++;Note($"ERRO: {f} → {t}: {e.Message}");} Note($"Cópias concluídas: {copied}; ignoradas: {skipped}; erros: {err}.");Alert(status.Text+"\nConsulte a aba Relatório para os detalhes."); } catch(Exception e) {Alert(e.Message);} }
    void previewCopy(List<string> lines) { using var f=new Form { Text=$"Prévia da cópia — {lines.Count} operação(ões)", Size=new Size(950,600), StartPosition=FormStartPosition.CenterParent }; var t=new TextBox { Dock=DockStyle.Fill, Multiline=true, ReadOnly=true, ScrollBars=ScrollBars.Both, WordWrap=false, Lines=lines.ToArray() }; f.Controls.Add(t); f.ShowDialog(this); }
    void SaveLog() { using var d=new SaveFileDialog { Filter="Arquivo de texto (*.txt)|*.txt",FileName="relatorio-pastafacil.txt" }; if(d.ShowDialog()==DialogResult.OK)try {File.WriteAllText(d.FileName,log.Text,Encoding.UTF8);Alert("Relatório salvo.");}catch(Exception e){Alert(e.Message);} }
}
