# PowerShell script to read Software Engineering Principles Word documents
$word = New-Object -ComObject Word.Application
$word.Visible = $false

$files = @(
    "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Computer Science\Software Engineering Principles\Coursework\COMP2912 2023 Coursework 1(1).docx",
    "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Computer Science\Software Engineering Principles\Coursework\Software Design Brief.docx"
)

foreach ($file in $files) {
    Write-Host "=== Reading: $file ===" -ForegroundColor Green
    try {
        $doc = $word.Documents.Open($file)
        $text = $doc.Content.Text
        Write-Output $text
        $doc.Close()
    }
    catch {
        Write-Host "Error reading file: $file" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }
    Write-Host "`n`n" -ForegroundColor Yellow
}

$word.Quit()