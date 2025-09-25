# PowerShell script to convert Software Engineering Project documents to PDF
$word = New-Object -ComObject Word.Application
$word.Visible = $false

# Convert Software Engineering Project assessment brief
$docPath1 = "C:\Users\Abdua\Programs\Personal\University-portfolio-work\Year 2\Software Engineering Project\Assessment Brief CW2(1).pdf"
$pdfPath1 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\software-engineering-assessment-brief.pdf"

# Convert marking rubric
$docPath2 = "C:\Users\Abdua\Programs\Personal\University-portfolio-work\Year 2\Software Engineering Project\COMP2913_marking_repository_rubric(1).pdf"
$pdfPath2 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\software-engineering-marking-rubric.pdf"

try {
    Write-Host "Copying Software Engineering Assessment Brief..." -ForegroundColor Green
    Copy-Item $docPath1 $pdfPath1 -Force
    Write-Host "Assessment Brief copied successfully to: $pdfPath1" -ForegroundColor Green

    Write-Host "Copying Software Engineering Marking Rubric..." -ForegroundColor Green
    Copy-Item $docPath2 $pdfPath2 -Force
    Write-Host "Marking Rubric copied successfully to: $pdfPath2" -ForegroundColor Green
}
catch {
    Write-Host "Error copying documents: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $word.Quit()
}