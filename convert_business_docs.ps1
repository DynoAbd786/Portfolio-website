# PowerShell script to convert Business coursework documents to PDF
$word = New-Object -ComObject Word.Application
$word.Visible = $false

# Convert main coursework submission (rename from student ID)
$docPath1 = "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Business\Starting Your Own Business\Coursework\201617302.docx"
$pdfPath1 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\project-synapse-business-plan.pdf"

# Convert assignment brief
$docPath2 = "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Business\Starting Your Own Business\Coursework\LUBS1890_Assignment Brief_23-24(1).docx"
$pdfPath2 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\business-assignment-brief.pdf"

try {
    Write-Host "Converting Project Synapse Business Plan..." -ForegroundColor Green
    $doc1 = $word.Documents.Open($docPath1)
    $doc1.SaveAs2($pdfPath1, 17)  # 17 is the PDF format
    $doc1.Close()
    Write-Host "Business Plan PDF created successfully at: $pdfPath1" -ForegroundColor Green

    Write-Host "Converting Assignment Brief..." -ForegroundColor Green
    $doc2 = $word.Documents.Open($docPath2)
    $doc2.SaveAs2($pdfPath2, 17)
    $doc2.Close()
    Write-Host "Assignment Brief PDF created successfully at: $pdfPath2" -ForegroundColor Green
}
catch {
    Write-Host "Error converting documents: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $word.Quit()
}