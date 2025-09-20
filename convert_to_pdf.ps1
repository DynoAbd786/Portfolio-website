# PowerShell script to convert Word document to PDF
$word = New-Object -ComObject Word.Application
$word.Visible = $false

$docPath = "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 1\Chemical engineering\Nanotechnology\Coursework 1 - Report\The Investment for Quantum Dot Technology for the Use of Quantum Computing with Light.docx"
$pdfPath = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\quantum-dot-investment-report.pdf"

$doc = $word.Documents.Open($docPath)
$doc.SaveAs2($pdfPath, 17)  # 17 is the PDF format
$doc.Close()
$word.Quit()

Write-Host "PDF created successfully at: $pdfPath"