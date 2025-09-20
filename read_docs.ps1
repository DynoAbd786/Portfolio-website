# PowerShell script to read Word documents
$word = New-Object -ComObject Word.Application
$word.Visible = $false

$files = @(
    "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 1\Chemical engineering\Nanotechnology\Coursework 1 - Report\The Investment for Quantum Dot Technology for the Use of Quantum Computing with Light.docx",
    "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 1\Chemical engineering\Nanotechnology\Coursework 1 - Report\Plan.docx",
    "C:\Users\Abdua\OneDrive\Documents\Leeds Uni\Year 1\Chemical engineering\Nanotechnology\Coursework 1 - Report\ACW_2022_23_spin_out(1).docx"
)

foreach ($file in $files) {
    Write-Host "=== Reading: $file ===" -ForegroundColor Green
    $doc = $word.Documents.Open($file)
    $text = $doc.Content.Text
    Write-Output $text
    $doc.Close()
    Write-Host "`n`n" -ForegroundColor Yellow
}

$word.Quit()