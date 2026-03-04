#!/bin/bash

# Get all SVG files
echo "Scanning for SVG files..."
find wwwroot -name "*.svg" > /tmp/all_svgs.txt

# Search for references in code
echo "Checking for references in codebase..."
> /tmp/unused_svgs.txt
while IFS= read -r svg; do
    filename=$(basename "$svg")
    # Search in Views, CSS, JS, and other files
    refs=$(grep -r "$filename" --include="*.cshtml" --include="*.css" --include="*.js" --include="*.razor" Views/ wwwroot/ 2>/dev/null | wc -l)
    if [ $refs -eq 0 ]; then
        echo "$svg" >> /tmp/unused_svgs.txt
    fi
done < /tmp/all_svgs.txt

unused_count=$(wc -l < /tmp/unused_svgs.txt)
total_count=$(wc -l < /tmp/all_svgs.txt)

echo ""
echo "Total SVG files: $total_count"
echo "Unused SVG files: $unused_count"
echo ""
echo "Top 20 unused files:"
head -20 /tmp/unused_svgs.txt
echo ""
echo "Full list saved to /tmp/unused_svgs.txt"
