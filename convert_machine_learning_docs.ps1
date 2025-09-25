# PowerShell script to convert Machine Learning coursework documents to PDF
$word = New-Object -ComObject Word.Application
$word.Visible = $false

# Convert Machine Learning coursework brief
$docPath1 = "C:\Users\Abdua\Programs\Personal\University-portfolio-work\Year 3\Machine Learning\briefing.pdf"
$pdfPath1 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\machine-learning-coursework-brief.pdf"

try {
    Write-Host "Copying Machine Learning Coursework Brief..." -ForegroundColor Green
    Copy-Item $docPath1 $pdfPath1 -Force
    Write-Host "Coursework Brief copied successfully to: $pdfPath1" -ForegroundColor Green
}
catch {
    Write-Host "Error copying documents: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $word.Quit()
}