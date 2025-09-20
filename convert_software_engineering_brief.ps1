# PowerShell script to convert Software Engineering Design Brief to PDF
$word = New-Object -ComObject Word.Application
$word.Visible = $false

$docPath = "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Computer Science\Software Engineering Principles\Coursework\Software Design Brief.docx"
$pdfPath = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\software-engineering-design-brief.pdf"

try {
    $doc = $word.Documents.Open($docPath)
    $doc.SaveAs2($pdfPath, 17)  # 17 is the PDF format
    $doc.Close()
    Write-Host "PDF created successfully at: $pdfPath" -ForegroundColor Green
}
catch {
    Write-Host "Error converting document: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $word.Quit()
}