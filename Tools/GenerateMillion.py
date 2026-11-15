"""Expand the original sample into 250,000 product variants x four warehouses."""
from pathlib import Path
import sqlite3, json
root = Path(__file__).resolve().parents[1]
target = root / 'Data/warehouse-1000000.sqlite'
if target.exists(): raise SystemExit('Target already exists; preserved.')
source = sqlite3.connect(f"file:{root / 'Data/warehouse-100000.sqlite'}?mode=ro", uri=True)
products = source.execute('SELECT * FROM products ORDER BY code').fetchall()
balances = source.execute('SELECT * FROM stock_balances').fetchall()
c = sqlite3.connect(target)
for sql, in source.execute("SELECT sql FROM sqlite_master WHERE type='table'"): c.execute(sql)
c.execute("ALTER TABLE products ADD COLUMN category TEXT NOT NULL DEFAULT ''")
c.executemany('INSERT INTO warehouses VALUES (?,?)',source.execute('SELECT * FROM warehouses'))
# Prefix families retain the existing realistic product names; variants represent supplier lots/specifications.
labels = {'SRB':'Fasteners','KAR':'Packaging','ODZ':'Workwear','KAB':'Cables','PRZ':'Cables','ELE':'Electrical supplies','NAR':'Tools','BHP':'Safety equipment'}
for variant in range(10):
 suffix = '' if variant == 0 else f'-V{variant+1:02}'
 c.executemany('INSERT INTO products VALUES (?,?,?,?,?)',[(i+suffix,code+suffix,name+(f' — wariant dostawcy {variant+1}' if variant else ''),unit,labels.get(code.split('-')[0], 'Other / '+code.split('-')[0])) for i,code,name,unit in products])
 c.executemany('INSERT INTO stock_balances VALUES (?,?,?,?)',[(w,p+suffix,onhand,reserved) for w,p,onhand,reserved in balances])
c.commit()
assert c.execute('SELECT COUNT(*) FROM stock_balances').fetchone()[0] == 1000000
assert c.execute('PRAGMA integrity_check').fetchone()[0] == 'ok'
assert not c.execute('PRAGMA foreign_key_check').fetchall()
c.close();source.close()
print(target, target.stat().st_size)
