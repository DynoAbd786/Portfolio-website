# PowerShell script to convert AI coursework documents to PDF
$word = New-Object -ComObject Word.Application
$word.Visible = $false

# Convert Search Algorithms coursework documents
$docPath1 = "C:\Users\Abdua\Programs\Personal\University-portfolio-work\Year 2\AI\Coursework 1\2023_24_AI_Search_Coursework.pdf"
$pdfPath1 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\ai-search-algorithms-brief.pdf"

$docPath2 = "C:\Users\Abdua\Programs\Personal\University-portfolio-work\Year 2\AI\Coursework 1\Assignment 1.pdf"
$pdfPath2 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\ai-search-algorithms-submission.pdf"

# Convert Decision Trees coursework documents
$docPath3 = "C:\Users\Abdua\Programs\Personal\University-portfolio-work\Year 2\AI\Coursework 2\Assessment Brief.pdf"
$pdfPath3 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\ai-decision-trees-brief.pdf"

try {
    Write-Host "Copying AI Search Algorithms Assignment Brief..." -ForegroundColor Green
    Copy-Item $docPath1 $pdfPath1 -Force
    Write-Host "Search Algorithms Brief copied successfully to: $pdfPath1" -ForegroundColor Green

    Write-Host "Copying AI Search Algorithms Submission..." -ForegroundColor Green
    Copy-Item $docPath2 $pdfPath2 -Force
    Write-Host "Search Algorithms Submission copied successfully to: $pdfPath2" -ForegroundColor Green

    Write-Host "Copying AI Decision Trees Assessment Brief..." -ForegroundColor Green
    Copy-Item $docPath3 $pdfPath3 -Force
    Write-Host "Decision Trees Brief copied successfully to: $pdfPath3" -ForegroundColor Green
}
catch {
    Write-Host "Error copying documents: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $word.Quit()
}