# PowerShell script to read Business coursework Word documents
$word = New-Object -ComObject Word.Application
$word.Visible = $false

$files = @(
    "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Business\Starting Your Own Business\Coursework\LUBS1890_Assignment Brief_23-24(1).docx",
    "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Business\Starting Your Own Business\Coursework\201617302.docx",
    "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 2\Business\Starting Your Own Business\Coursework\Plan.docx"
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