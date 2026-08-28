# Stampante virtuale su Windows

Su Windows **FinePdf non installa nessuna stampante**. Ci si appoggia a una **stampante PDF
gratuita di terzi** che sa salvare in una cartella senza chiedere il nome file; FinePdf si
limita a **osservare quella cartella** e a mostrarne i PDF nella scheda **«PDF da stampante»**.

## Passi

1. **Installa una stampante PDF gratuita** che supporti il salvataggio automatico in una
   cartella (senza finestra "Salva con nome"):
   - **Xodo PDF Printer** — <https://www.xodo.com/pdf-printer> (impostazione: *Output* →
     *Auto-save to folder*).
   - **Documalis Free PDF Printer** — <https://www.documalis.com/en/freepdfprinter.php>
     (opzione di salvataggio automatico in cartella).
   - Altre equivalenti vanno bene, purché offrano "salva in cartella senza prompt".
   > La **"Microsoft Print to PDF"** integrata in Windows chiede *sempre* il percorso a ogni
   > stampa → **non adatta** a questo scopo.

2. **Configura la stampante scelta** perché salvi in una cartella dedicata, es.
   `C:\Users\<tu>\FinePdf`.

3. **In FinePdf** apri **«Stampante virtuale…»** nella toolbar → scegli **la stessa cartella**
   → *Usa questa cartella*. In alternativa, nella scheda **«PDF da stampante»** usa
   **«Cambia cartella»**. Il percorso viene ricordato (`%APPDATA%\FinePdf\settings.json`,
   chiave `printerOutputFolder`).

## Uso

Stampa da qualsiasi app scegliendo la stampante PDF di terzi → il PDF finisce nella cartella →
compare subito nella scheda **«PDF da stampante»** di FinePdf → doppio click per accodarne le
pagine alla composizione.

## Note

- Lo **schema del nome file** dei PDF lo decide la stampante di terzi, non FinePdf.
- La qualità e la "silenziosità" della stampa dipendono dalla stampante scelta.
- Su **Linux** invece FinePdf installa una vera stampante CUPS: vedi
  [`virtual-printer.md`](virtual-printer.md).
