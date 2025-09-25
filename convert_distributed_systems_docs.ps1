# PowerShell script to convert Distributed Systems coursework documents to PDF
$word = New-Object -ComObject Word.Application
$word.Visible = $false

# Convert Distributed Systems coursework brief
$docPath1 = "C:\Users\Abdua\Programs\Personal\University-portfolio-work\Year 3\Distributed Systems\cwk2(1).pdf"
$pdfPath1 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\distributed-systems-coursework-brief.pdf"

try {
    Write-Host "Copying Distributed Systems Coursework Brief..." -ForegroundColor Green
    Copy-Item $docPath1 $pdfPath1 -Force
    Write-Host "Coursework Brief copied successfully to: $pdfPath1" -ForegroundColor Green
}
catch {
    Write-Host "Error copying documents: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $word.Quit()
}