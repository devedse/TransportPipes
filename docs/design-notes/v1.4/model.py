# The v1.4 content model: prices, recipes, value per tile per Era
P = {  # base prices
 'energy':1,'water':0.5,'ironOre':3,'wood':1.5,'copperOre':3.5,'coal':2.5,'stone':1.5,'sand':1.2,
 'ironIngot':14,'copperIngot':18,'bricks':5,'glass':14,'planks':3.5,
 'bauxite':8,'steel':60,'copperWire':40,'concrete':20,'aluminium':80,
 'oil':25,'plastic':150,'gears':290,'motors':700,
 'rareEarth':300,'circuits':1500,'batteries':1800,'robots':7700,
 'titaniumOre':1300,'titanium':4800,'superconductors':10000,'spaceframes':34000,
}
# id: (name, era, size, kind, inputs, outputs, payback_min, materials)
B = [
 # Era 1 power
 ('Solar Panel',1,1,'power',{}, {'energy':5}),
 ('Wind Moily',1,1,'power',{}, {'energy':30}),
 ('Hydro Wheel',1,1,'power',{}, {'energy':40}),
 ('Coal Plant',1,4,'power',{'coal':10,'water':20}, {'energy':200}),
 # Era 1 extraction
 ('Iron Mine',1,1,'mine',{'energy':10},{'ironOre':12}),
 ('Copper Mine',1,1,'mine',{'energy':12},{'copperOre':10}),
 ('Coal Pit',1,1,'mine',{'energy':8},{'coal':12}),
 ('Quarry',1,4,'mine',{'energy':25},{'stone':80}),
 ('Sand Dredger',1,1,'mine',{'energy':6},{'sand':25}),
 ('Lumber Camp',1,1,'mine',{'energy':6},{'wood':20}),
 ('Water Pump',1,1,'mine',{'energy':3},{'water':50}),
 # Era 1 factories
 ('Small Factory',1,1,'factory',{'energy':20,'ironOre':12},{'ironIngot':6}),
 ('Big Factory',1,4,'factory',{'energy':90,'ironOre':60},{'ironIngot':28}),
 ('Smelter',1,1,'factory',{'energy':20,'copperOre':10,'coal':4},{'copperIngot':5}),
 ('Brickworks',1,1,'factory',{'energy':10,'stone':10,'coal':5},{'bricks':12}),
 ('Glassworks',1,1,'factory',{'energy':25,'sand':15},{'glass':5}),
 ('Sawmill',1,1,'factory',{'energy':6,'wood':10},{'planks':12}),
 # Era 2
 ('Big Turbine',2,4,'power',{}, {'energy':400}),
 ('Steam Turbine',2,4,'power',{'coal':30,'water':40}, {'energy':500}),
 ('Bauxite Pit',2,1,'mine',{'energy':20},{'bauxite':15}),
 ('Deep Mine',2,4,'mine',{'energy':60},{'ironOre':160}),
 ('Steel Mill',2,4,'factory',{'energy':120,'ironIngot':20,'coal':10},{'steel':14}),
 ('Wire Mill',2,1,'factory',{'energy':40,'copperIngot':6},{'copperWire':6}),
 ('Cement Kiln',2,4,'factory',{'energy':50,'stone':20,'sand':20,'water':10},{'concrete':25}),
 ('Aluminium Works',2,4,'factory',{'energy':300,'bauxite':30},{'aluminium':12}),
 # Era 3
 ('Solar Farm',3,9,'power',{}, {'energy':3600}),
 ('Oil Derrick',3,1,'mine',{'energy':30},{'oil':18}),
 ('Refinery',3,9,'factory',{'energy':600,'oil':60,'water':40},{'plastic':38}),
 ('Machine Shop',3,4,'factory',{'energy':400,'steel':20},{'gears':11}),
 ('Motor Works',3,4,'factory',{'energy':500,'copperWire':10,'aluminium':6,'gears':2},{'motors':5}),
 # Era 4
 ('Geothermal Plant',4,4,'power',{'water':20}, {'energy':6400}),
 ('Rare Earth Excavator',4,4,'mine',{'energy':200},{'rareEarth':22}),
 ('Circuit Fab',4,4,'factory',{'energy':1500,'copperWire':20,'plastic':10,'glass':10},{'circuits':7}),
 ('Battery Works',4,4,'factory',{'energy':1500,'rareEarth':6,'aluminium':10},{'batteries':6}),
 ('Robot Works',4,9,'factory',{'energy':4000,'motors':4,'circuits':4,'batteries':2},{'robots':4}),
 # Era 5
 ('Fusion Reactor',5,9,'power',{'superconductors':1,'water':100}, {'energy':67000}),
 ('Titanium Mine',5,4,'mine',{'energy':400},{'titaniumOre':20}),
 ('Titanium Works',5,4,'factory',{'energy':6000,'titaniumOre':20},{'titanium':12}),
 ('Superconductor Lab',5,4,'factory',{'energy':6000,'rareEarth':10,'circuits':4,'copperWire':20},{'superconductors':4}),
 ('Spaceframe Works',5,9,'factory',{'energy':15000,'titanium':6,'robots':2,'superconductors':2},{'spaceframes':4}),
]
def val(d): return sum(P[k]*v for k,v in d.items())
if __name__=='__main__':
    for name,era,size,kind,i,o in B:
        va=val(o)-val(i)
        share = i.get('energy',0)/val(i) if val(i) else 0
        print(f"{era} {name:22s} {size} out {val(o):>9.0f} in {val(i):>8.0f} VA/tile {va/size:>8.0f}  target {25*4**(era-1):>6}  energy share {share:.0%}")

# Payback at level 1 (minutes) and build time per Era; costs for the Era-1 buildings stay as today's
PAYBACK = {2: 90, 3: 180, 4: 360, 5: 720}
TODAY = {'Solar Panel':30,'Wind Moily':200,'Hydro Wheel':1200,'Coal Plant':6000,'Iron Mine':600,'Copper Mine':900,
         'Coal Pit':700,'Quarry':3000,'Sand Dredger':500,'Lumber Camp':400,'Water Pump':300,'Small Factory':1500,
         'Big Factory':12000,'Smelter':1800,'Brickworks':1400,'Glassworks':1600,'Sawmill':800}
MATS = {2: ('bricks','planks'), 3: ('steel','concrete'), 4: ('aluminium','steel'), 5: ('circuits','aluminium')}
def nice(x):
    import math
    if x < 1000: return round(x, -1)
    e = 10 ** (int(math.log10(x)) - 1)
    return round(x / e) * e
def cost(b):
    name, era, size, kind, i, o = b
    if era == 1: return TODAY[name], {}
    va = val(o) - val(i)
    gold = nice(va * PAYBACK[era])
    m1, m2 = MATS[era]
    share = gold * 0.2
    return gold, {m1: nice(share * 0.6 / P[m1]), m2: nice(share * 0.4 / P[m2])}
