# PowerShell script to convert Data Mining coursework documents to PDF
$word = New-Object -ComObject Word.Application
$word.Visible = $false

# Convert assignment brief
$docPath1 = "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Computer Science\Data Mining\Coursework\COMP2121AssessmentBrief2024.pdf"
$pdfPath1 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\data-mining-assignment-brief.pdf"

# Convert research proposal (already PDF, just copy)
$docPath2 = "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Computer Science\Data Mining\Coursework\MuhammadKashif-Khan.pdf"
$pdfPath2 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\medical-ai-research-proposal.pdf"

# Convert Gantt chart PDF
$docPath3 = "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Computer Science\Data Mining\Coursework\EPSRC_Research_Proposal_Gantt_Chart.pdf"
$pdfPath3 = "C:\Users\Abdua\Programs\Personal\Portfolio-website\website\wwwroot\docs\research-proposal-gantt-chart.pdf"

try {
    Write-Host "Copying Data Mining Assignment Brief..." -ForegroundColor Green
    Copy-Item $docPath1 $pdfPath1 -Force
    Write-Host "Assignment Brief copied successfully to: $pdfPath1" -ForegroundColor Green

    Write-Host "Copying Medical AI Research Proposal..." -ForegroundColor Green
    Copy-Item $docPath2 $pdfPath2 -Force
    Write-Host "Research Proposal copied successfully to: $pdfPath2" -ForegroundColor Green

    Write-Host "Copying Gantt Chart..." -ForegroundColor Green
    Copy-Item $docPath3 $pdfPath3 -Force
    Write-Host "Gantt Chart copied successfully to: $pdfPath3" -ForegroundColor Green
}
catch {
    Write-Host "Error copying documents: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $word.Quit()
}