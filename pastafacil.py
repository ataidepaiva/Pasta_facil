import csv
import io
import os
import re
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, ttk

INVALID = re.compile(r'[<>:"/\\|?*\x00-\x1f]')
RESERVED = re.compile(r'^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?$', re.I)


def safe_name(value):
    value = value.strip()
    if not value or value in ('.', '..') or value.endswith(('.', ' ')) or INVALID.search(value) or RESERVED.match(value):
        raise ValueError(f'Nome de pasta inválido: {value!r}')
    return value


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title('PastaFácil — Gerador de Pastas e Subpastas')
        self.geometry('1000x720')
        self.minsize(760, 550)
        self.rows = []
        self.headers = []
        self.order = []
        self.paths = []
        self.destination = tk.StringVar(value=str(Path.home() / 'Documents'))
        self.has_header = tk.BooleanVar(value=True)
        self.build()

    def build(self):
        main = ttk.Frame(self, padding=16)
        main.pack(fill='both', expand=True)
        ttk.Label(main, text='PastaFácil', font=('Segoe UI', 22, 'bold')).pack(anchor='w')
        ttk.Label(main, text='Cole células do Excel ou Google Sheets. Cada linha representa um caminho de pastas.').pack(anchor='w', pady=(0, 10))
        self.data = tk.Text(main, height=9, wrap='none', undo=True)
        self.data.pack(fill='x')
        bar = ttk.Frame(main)
        bar.pack(fill='x', pady=8)
        ttk.Checkbutton(bar, text='A primeira linha contém os títulos das colunas', variable=self.has_header).pack(side='left')
        ttk.Button(bar, text='Ler colunas', command=self.parse).pack(side='right')
        ttk.Label(main, text='Colunas selecionadas (na ordem das pastas):').pack(anchor='w', pady=(8, 3))
        selection = ttk.Frame(main)
        selection.pack(fill='x')
        self.columns = tk.Listbox(selection, height=6, selectmode='browse', exportselection=False)
        self.columns.pack(side='left', fill='x', expand=True)
        buttons = ttk.Frame(selection)
        buttons.pack(side='left', padx=8)
        for title, action in [('↑ Subir', self.up), ('↓ Descer', self.down), ('Remover', self.remove), ('Adicionar', self.add)]:
            ttk.Button(buttons, text=title, command=action).pack(fill='x', pady=2)
        ttk.Label(main, text='Pasta de destino:').pack(anchor='w', pady=(12, 3))
        dest = ttk.Frame(main)
        dest.pack(fill='x')
        ttk.Entry(dest, textvariable=self.destination).pack(side='left', fill='x', expand=True)
        ttk.Button(dest, text='Escolher...', command=self.choose).pack(side='left', padx=(8, 0))
        actions = ttk.Frame(main)
        actions.pack(fill='x', pady=10)
        ttk.Button(actions, text='Atualizar prévia', command=self.preview).pack(side='left')
        ttk.Button(actions, text='Criar pastas', command=self.create).pack(side='right')
        ttk.Label(main, text='Prévia dos caminhos (nenhuma pasta é criada nesta etapa):').pack(anchor='w')
        self.tree = ttk.Treeview(main, columns=('path',), show='tree')
        self.tree.pack(fill='both', expand=True, pady=(4, 0))
        self.status = tk.StringVar(value='Cole os dados para começar.')
        ttk.Label(main, textvariable=self.status).pack(anchor='w', pady=(8, 0))

    def parse(self):
        content = self.data.get('1.0', 'end-1c').strip('\n\r')
        if not content.strip():
            messagebox.showwarning('Dados ausentes', 'Cole os dados da planilha primeiro.')
            return False
        try:
            records = list(csv.reader(io.StringIO(content), delimiter='\t'))
            records = [r for r in records if any(c.strip() for c in r)]
            width = max(map(len, records))
            records = [r + [''] * (width - len(r)) for r in records]
            if self.has_header.get():
                self.headers = [x.strip() or f'Coluna {i+1}' for i, x in enumerate(records[0])]
                self.rows = records[1:]
            else:
                self.headers = [f'Coluna {i+1}' for i in range(width)]
                self.rows = records
            if not self.rows:
                raise ValueError('Não há linhas de dados após o cabeçalho.')
            self.order = list(range(width))
            self.refresh_columns()
            self.paths = []
            self.status.set(f'{len(self.rows)} linhas e {width} colunas identificadas.')
            return True
        except Exception as exc:
            messagebox.showerror('Erro na leitura', str(exc))
            return False

    def refresh_columns(self, selected=0):
        self.columns.delete(0, 'end')
        for i in self.order:
            self.columns.insert('end', f'{i+1}. {self.headers[i]}')
        if self.order:
            self.columns.selection_set(min(selected, len(self.order)-1))

    def selected(self):
        selected = self.columns.curselection()
        return selected[0] if selected else None

    def up(self):
        i = self.selected()
        if i is not None and i > 0:
            self.order[i-1], self.order[i] = self.order[i], self.order[i-1]
            self.refresh_columns(i-1)

    def down(self):
        i = self.selected()
        if i is not None and i < len(self.order)-1:
            self.order[i+1], self.order[i] = self.order[i], self.order[i+1]
            self.refresh_columns(i+1)

    def remove(self):
        i = self.selected()
        if i is not None:
            self.order.pop(i)
            self.refresh_columns(i)

    def add(self):
        missing = [i for i in range(len(self.headers)) if i not in self.order]
        if not missing:
            messagebox.showinfo('Colunas', 'Todas as colunas já estão selecionadas.')
            return
        dialog = tk.Toplevel(self)
        dialog.title('Adicionar coluna')
        dialog.transient(self)
        dialog.grab_set()
        ttk.Label(dialog, text='Escolha a coluna:').pack(padx=16, pady=8)
        choice = ttk.Combobox(dialog, state='readonly', values=[f'{i+1}. {self.headers[i]}' for i in missing])
        choice.pack(padx=16, pady=8)
        choice.current(0)
        def confirm():
            self.order.append(missing[choice.current()])
            self.refresh_columns(len(self.order)-1)
            dialog.destroy()
        ttk.Button(dialog, text='Adicionar', command=confirm).pack(pady=10)

    def choose(self):
        directory = filedialog.askdirectory(title='Selecione a pasta de destino')
        if directory:
            self.destination.set(directory)

    def preview(self):
        if not self.rows:
            if not self.parse():
                return False
        if not self.order:
            messagebox.showwarning('Colunas', 'Selecione pelo menos uma coluna.')
            return False
        destination = Path(self.destination.get()).expanduser()
        if not destination.is_dir():
            messagebox.showerror('Destino', 'Escolha uma pasta de destino que já exista.')
            return False
        paths = set()
        errors = []
        for number, row in enumerate(self.rows, start=2 if self.has_header.get() else 1):
            try:
                values = [safe_name(row[i]) for i in self.order]
                path = destination
                for value in values:
                    path = path / value
                    paths.add(path)
            except ValueError as exc:
                errors.append(f'Linha {number}: {exc}')
        if errors:
            messagebox.showerror('Corrija os dados', '\n'.join(errors[:12]) + ('\n...' if len(errors)>12 else ''))
            return False
        self.paths = sorted(paths, key=lambda p: (len(p.parts), str(p).casefold()))
        self.tree.delete(*self.tree.get_children())
        ids = {}
        for path in self.paths:
            parent = ids.get(path.parent, '')
            ids[path] = self.tree.insert(parent, 'end', text=path.name, open=(len(path.parts)-len(destination.parts) <= 2))
        existing = sum(p.is_dir() for p in self.paths)
        self.status.set(f'{len(self.paths)} pastas na estrutura; {existing} já existem; {len(self.paths)-existing} a criar.')
        return True

    def create(self):
        if not self.preview():
            return
        if not messagebox.askyesno('Confirmar criação', f'Criar as pastas em:\n{self.destination.get()}\n\nTotal de caminhos: {len(self.paths)}\n\nNenhuma pasta existente será apagada.'):
            return
        created = existing = 0
        errors = []
        for path in self.paths:
            try:
                if path.is_dir():
                    existing += 1
                elif path.exists():
                    errors.append(f'{path}: já existe um arquivo com este nome')
                else:
                    path.mkdir(exist_ok=True)
                    created += 1
            except OSError as exc:
                errors.append(f'{path}: {exc}')
        result = f'Pastas criadas: {created}\nPastas já existentes: {existing}\nErros: {len(errors)}'
        self.status.set(result.replace('\n', ' | '))
        if errors:
            messagebox.showwarning('Resultado', result + '\n\n' + '\n'.join(errors[:10]))
        else:
            messagebox.showinfo('Concluído', result)


if __name__ == '__main__':
    App().mainloop()
