$html = Get-Content -LiteralPath 'C:\Users\Great-Lucky\AppData\Local\Temp\swstats.html' -Raw
$rows = @()
$blocks = [regex]::Matches($html, '<tr class="searchable".*?</tr>', [Text.RegularExpressions.RegexOptions]::Singleline)
foreach ($match in $blocks) {
  $block = $match.Value
  $name = [regex]::Match($block, '<span class="element">(.*?)</span><h3>(.*?)</h3>', [Text.RegularExpressions.RegexOptions]::Singleline)
  $values = [regex]::Matches($block, '<td class="stat">\s*([0-9-]+)\s*</td>')
  if (-not $name.Success -or $values.Count -lt 8) { continue }
  $max = @($values[4].Groups[1].Value, $values[5].Groups[1].Value, $values[6].Groups[1].Value, $values[7].Groups[1].Value)
  if ($max -contains '-') { continue }
  $family = [Net.WebUtility]::HtmlDecode(([regex]::Replace($name.Groups[1].Value, '<.*?>', ''))).Trim()
  $monster = [Net.WebUtility]::HtmlDecode(([regex]::Replace($name.Groups[2].Value, '<.*?>', ''))).Trim()
  $rows += [pscustomobject]@{ key = "$family $monster"; hp = [int]$max[0]; atk = [int]$max[1]; def = [int]$max[2]; spd = [int]$max[3] }
}
$rows | ConvertTo-Json -Compress
