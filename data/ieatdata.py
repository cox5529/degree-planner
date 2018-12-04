# -*- coding: cp1252 -*-
from bs4 import BeautifulSoup
import urllib2

def get(deps):
        f = open('names.txt','w')
        for dep in deps:
                page = urllib2.urlopen('http://catalog.uark.edu/undergraduatecatalog/coursesofinstruction/'+str(dep)+'/').read()
                soup = BeautifulSoup(page)
                for link in soup.find_all('p','courseblocktitle'):
                        data = link.string
                        output = ''
                        space = False
                        dot = 0
                        skip = 0
                        for ch in data:
                                if skip > 0:
                                        skip -= 1
                                        continue
                                if ord(ch)==ord(' ') and not space:
                                        space = True
                                elif ord(ch)==ord('.'):
                                        dot+=1
                                        if dot > 1:
                                                break
                                        output+=str(',')
                                        skip = 2
                                else:
                                        output+=ch
                        output+='\n'
                        f.write(output)
        f.close()
