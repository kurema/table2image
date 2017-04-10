open(html, '<', $ARGV[0]) or die 'Cannot open file: $!';
my @temp=<html>;
$text=join "",@temp;

$cnt=0;
while($text=~ /(<table>.+?<\/table>)/s){
	my $content=$1;
	my $img="<img src=\"imgt/".$cnt.".jpeg\"/>";
	$text=~ s/\Q$content\E/$img/mg;
	open(table, '>', 'table/'.$cnt.'.html') or die 'Cannot open file: $!';
	print table $content;
	close table;
	$cnt++;
}

open(htmlout, '>', $ARGV[1]) or die 'Cannot open file: $!';
print htmlout $text;
