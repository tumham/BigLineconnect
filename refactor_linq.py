import re
import glob

def refactor_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # We want to replace "BigusAktarimDataContext db = new BigusAktarimDataContext(conn);"
    # with "using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn)) \n { \n"
    # and then add the closing brace "}" right before the last "}" of the method.
    
    # A safer approach:
    # Find all function declarations that contain the target string.
    # regex to find the method signature, block start '{', followed by the db initialization.
    
    pattern = r'(public static.*?)\n\s*\{\s*\n\s*BigusAktarimDataContext db = new BigusAktarimDataContext\(conn\);(.*?)^\s*\}'
    
    # Actually, Python's re is tricky for overlapping/nested braces. 
    # Let's do a simple string manipulation:
    # For every "BigusAktarimDataContext db = new BigusAktarimDataContext(conn);", replace it with:
    # using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn)) {
    # And then we need to insert `}` before `return... }` 
    
    # Let's write a simple state machine to parse and fix this.
    lines = content.split('\n')
    new_lines = []
    
    inside_method = False
    brace_count = 0
    added_using = False
    
    for line in lines:
        if 'BigusAktarimDataContext db = new BigusAktarimDataContext(conn);' in line:
            indent = line[:line.find('BigusAktarimDataContext')]
            new_lines.append(indent + 'using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))')
            new_lines.append(indent + '{')
            added_using = True
            brace_count = 1
            inside_method = True
        elif inside_method:
            if '{' in line:
                brace_count += line.count('{')
            if '}' in line:
                brace_count -= line.count('}')
                
            if brace_count == 0 and added_using: # reached end of method
                # Insert ending brace
                indent = line[:line.find('}')]
                if indent.strip() == '':
                    new_lines.append(indent + '    }')
                else:
                    new_lines.append('            }')
                new_lines.append(line)
                added_using = False
                inside_method = False
            else:
                new_lines.append(line)
        else:
            new_lines.append(line)
            
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write('\n'.join(new_lines))

files = glob.glob(r'c:\Projev17YD\DUZ_V17_STD\Bigus.Aktarici.Linq\*.cs')
for file in files:
    if 'Aktarim' in file or 'Kart' in file:
        refactor_file(file)
print("Done refactoring.")
