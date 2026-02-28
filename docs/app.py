import fitz

doc = fitz.open(r"C:\Programmes\C#\SPC\docs\doc_DB_SPC_SI_Queries.pdf")
text = "\n".join(page.get_text() for page in doc)
with open("doc_DB_SPC_SI_Queries.txt", "w", encoding="utf-8") as f:
    f.write(text)