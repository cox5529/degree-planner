courses = open('courses_in_db.txt','r')
names = open('names.txt','r')
output = open('course_names.txt','w')

course = courses.readline()
name = names.readline()

while course is not '':
    listing = course[0:course.find(',')]
    found = False
    while name is not '':
        nlisting = name[0:name.find(',')]
        print listing + ' ' + nlisting
        if nlisting == listing:
            output.write(nlisting+',"'+name[name.find(',')+1:name.find('(')]+'"')
            output.write('\n')
            #output.write(name)
            found = True
            break
        if nlisting > listing:
            break
        name = names.readline()
    if not found:
        output.write(listing)
        output.write('\n')
    course = courses.readline()
'''
for course in courses:
    listing = course[0:course.find(',')]
    found = False
    for name in names:
        nlisting = name[0:name.find(',')]
        print listing + ' ' + nlisting
        if nlisting == listing:
            output.write(name)
            output.write('\n')
            found = True
            break
        if nlisting > listing:
            break
    if not found:
        output.write(listing)
        output.write('\n')
'''    

output.close()
names.close()
courses.close()
