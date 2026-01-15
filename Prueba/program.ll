; ModuleID = 'RedLangModule'
source_filename = "RedLangModule"

@fmt_num = private unnamed_addr constant [5 x i8] c"%ld\0A\00", align 1
@str_lit = private unnamed_addr constant [46 x i8] c"La variable x es mas grande que la variable y\00", align 1
@fmt_str = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@str_lit.1 = private unnamed_addr constant [46 x i8] c"La variable y es mas grande que la variable x\00", align 1
@fmt_str.2 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@str_lit.3 = private unnamed_addr constant [11 x i8] c"While loop\00", align 1
@fmt_str.4 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@str_lit.5 = private unnamed_addr constant [6 x i8] c"Suma:\00", align 1
@fmt_str.6 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@fmt_num.7 = private unnamed_addr constant [5 x i8] c"%ld\0A\00", align 1
@str_lit.8 = private unnamed_addr constant [7 x i8] c"Resta:\00", align 1
@fmt_str.9 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@fmt_num.10 = private unnamed_addr constant [5 x i8] c"%ld\0A\00", align 1
@str_lit.11 = private unnamed_addr constant [17 x i8] c"Multiplicaci\C3\B3n:\00", align 1
@fmt_str.12 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@fmt_num.13 = private unnamed_addr constant [5 x i8] c"%ld\0A\00", align 1
@str_lit.14 = private unnamed_addr constant [11 x i8] c"Divisi\C3\B3n:\00", align 1
@fmt_str.15 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@fmt_num.16 = private unnamed_addr constant [5 x i8] c"%ld\0A\00", align 1
@str_lit.17 = private unnamed_addr constant [9 x i8] c"M\C3\B3dulo:\00", align 1
@fmt_str.18 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@fmt_num.19 = private unnamed_addr constant [5 x i8] c"%ld\0A\00", align 1
@str_lit.20 = private unnamed_addr constant [17 x i8] c"Ingresa un dato:\00", align 1
@fmt_str.21 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@fmt_scan_s = private unnamed_addr constant [3 x i8] c"%s\00", align 1
@str_lit.22 = private unnamed_addr constant [12 x i8] c"Ingresaste:\00", align 1
@fmt_str.23 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@fmt_str.24 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@str_lit.25 = private unnamed_addr constant [11 x i8] c"Factorial:\00", align 1
@fmt_str.26 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@fmt_num.27 = private unnamed_addr constant [5 x i8] c"%ld\0A\00", align 1
@str_lit.28 = private unnamed_addr constant [22 x i8] c"Hello from HelperLib!\00", align 1
@fmt_str.29 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1

declare i32 @printf(ptr, ...)

declare i32 @scanf(ptr, ...)

declare i32 @puts(ptr)

declare i32 @fflush(ptr)

declare i64 @atoll(ptr)

declare double @atof(ptr)

declare i32 @strcmp(ptr, ptr)

declare ptr @malloc(i64)

define i64 @Program_Main() {
entry:
  %z = alloca ptr, align 8
  store i64 0, ptr %z, align 4
  %x = alloca i64, align 8
  store i64 5, ptr %x, align 4
  %y = alloca i64, align 8
  store i64 3, ptr %y, align 4
  %j = alloca i64, align 8
  store i64 0, ptr %j, align 4
  br label %loopCond

loopCond:                                         ; preds = %loopBody, %entry
  %j1 = load i64, ptr %j, align 4
  %lttmp = icmp slt i64 %j1, 10
  br i1 %lttmp, label %loopBody, label %loopEnd

loopBody:                                         ; preds = %loopCond
  %j2 = load i64, ptr %j, align 4
  %y3 = load i64, ptr %y, align 4
  %addtmp = add i64 %j2, %y3
  %print_tmp = call i32 (ptr, ...) @printf(ptr @fmt_num, i64 %addtmp)
  %j4 = load i64, ptr %j, align 4
  %addtmp5 = add i64 %j4, 1
  store i64 %addtmp5, ptr %j, align 4
  br label %loopCond

loopEnd:                                          ; preds = %loopCond
  %x6 = load i64, ptr %x, align 4
  %y7 = load i64, ptr %y, align 4
  %gttmp = icmp sgt i64 %x6, %y7
  br i1 %gttmp, label %then, label %else

then:                                             ; preds = %loopEnd
  %print_tmp8 = call i32 (ptr, ...) @printf(ptr @fmt_str, ptr @str_lit)
  br label %merge

else:                                             ; preds = %loopEnd
  %print_tmp9 = call i32 (ptr, ...) @printf(ptr @fmt_str.2, ptr @str_lit.1)
  br label %merge

merge:                                            ; preds = %else, %then
  br label %repeatCond

repeatCond:                                       ; preds = %repeatBody, %merge
  %x10 = load i64, ptr %x, align 4
  %lttmp11 = icmp slt i64 %x10, 7
  br i1 %lttmp11, label %repeatBody, label %repeatEnd

repeatBody:                                       ; preds = %repeatCond
  %print_tmp12 = call i32 (ptr, ...) @printf(ptr @fmt_str.4, ptr @str_lit.3)
  %x13 = load i64, ptr %x, align 4
  %addtmp14 = add i64 %x13, 1
  store i64 %addtmp14, ptr %x, align 4
  br label %repeatCond

repeatEnd:                                        ; preds = %repeatCond
  %print_tmp15 = call i32 (ptr, ...) @printf(ptr @fmt_str.6, ptr @str_lit.5)
  %x16 = load i64, ptr %x, align 4
  %y17 = load i64, ptr %y, align 4
  %addtmp18 = add i64 %x16, %y17
  %print_tmp19 = call i32 (ptr, ...) @printf(ptr @fmt_num.7, i64 %addtmp18)
  %print_tmp20 = call i32 (ptr, ...) @printf(ptr @fmt_str.9, ptr @str_lit.8)
  %x21 = load i64, ptr %x, align 4
  %y22 = load i64, ptr %y, align 4
  %subtmp = sub i64 %x21, %y22
  %print_tmp23 = call i32 (ptr, ...) @printf(ptr @fmt_num.10, i64 %subtmp)
  %print_tmp24 = call i32 (ptr, ...) @printf(ptr @fmt_str.12, ptr @str_lit.11)
  %x25 = load i64, ptr %x, align 4
  %y26 = load i64, ptr %y, align 4
  %multmp = mul i64 %x25, %y26
  %print_tmp27 = call i32 (ptr, ...) @printf(ptr @fmt_num.13, i64 %multmp)
  %print_tmp28 = call i32 (ptr, ...) @printf(ptr @fmt_str.15, ptr @str_lit.14)
  %x29 = load i64, ptr %x, align 4
  %y30 = load i64, ptr %y, align 4
  %divtmp = sdiv i64 %x29, %y30
  %print_tmp31 = call i32 (ptr, ...) @printf(ptr @fmt_num.16, i64 %divtmp)
  %print_tmp32 = call i32 (ptr, ...) @printf(ptr @fmt_str.18, ptr @str_lit.17)
  %x33 = load i64, ptr %x, align 4
  %y34 = load i64, ptr %y, align 4
  %modtmp = srem i64 %x33, %y34
  %print_tmp35 = call i32 (ptr, ...) @printf(ptr @fmt_num.19, i64 %modtmp)
  %print_tmp36 = call i32 (ptr, ...) @printf(ptr @fmt_str.21, ptr @str_lit.20)
  %str_buffer = call ptr @malloc(i64 1024)
  store ptr %str_buffer, ptr %z, align 8
  %scan_tmp = call i32 (ptr, ...) @scanf(ptr @fmt_scan_s, ptr %str_buffer)
  %print_tmp37 = call i32 (ptr, ...) @printf(ptr @fmt_str.23, ptr @str_lit.22)
  %z38 = load ptr, ptr %z, align 8
  %print_tmp39 = call i32 (ptr, ...) @printf(ptr @fmt_str.24, ptr %z38)
  %obj = alloca ptr, align 8
  %objptr = call ptr @malloc(i64 1024)
  store ptr %objptr, ptr %obj, align 8
  %this_ptr = load ptr, ptr %obj, align 8
  %x40 = load i64, ptr %x, align 4
  %y41 = load i64, ptr %y, align 4
  %calltmp = call i64 @Math_suma(ptr %this_ptr, i64 %x40, i64 %y41)
  %test = alloca i64, align 8
  %this_ptr42 = load ptr, ptr %obj, align 8
  %x43 = load i64, ptr %x, align 4
  %y44 = load i64, ptr %y, align 4
  %calltmp45 = call i64 @Math_suma(ptr %this_ptr42, i64 %x43, i64 %y44)
  store i64 %calltmp45, ptr %test, align 4
  %this_ptr46 = load ptr, ptr %obj, align 8
  %x47 = load i64, ptr %x, align 4
  %y48 = load i64, ptr %y, align 4
  %calltmp49 = call i64 @Math_suma(ptr %this_ptr46, i64 %x47, i64 %y48)
  store i64 %calltmp49, ptr %x, align 4
  %print_tmp50 = call i32 (ptr, ...) @printf(ptr @fmt_str.26, ptr @str_lit.25)
  %this_ptr51 = load ptr, ptr %obj, align 8
  %x52 = load i64, ptr %x, align 4
  %calltmp53 = call i64 @Math_factorial(ptr %this_ptr51, i64 %x52)
  %print_tmp54 = call i32 (ptr, ...) @printf(ptr @fmt_num.27, i64 %calltmp53)
  ret i64 0
}

define i64 @Math_suma(ptr %0, i64 %a, i64 %c) {
entry:
  %a1 = alloca i64, align 8
  store i64 %a, ptr %a1, align 4
  %c2 = alloca i64, align 8
  store i64 %c, ptr %c2, align 4
  %a3 = load i64, ptr %a1, align 4
  %c4 = load i64, ptr %c2, align 4
  %addtmp = add i64 %a3, %c4
  ret i64 %addtmp
}

define i64 @Math_factorial(ptr %0, i64 %num) {
entry:
  %num1 = alloca i64, align 8
  store i64 %num, ptr %num1, align 4
  %num2 = load i64, ptr %num1, align 4
  %eqtmp = icmp eq i64 %num2, 1
  br i1 %eqtmp, label %then, label %merge

then:                                             ; preds = %entry
  %num3 = load i64, ptr %num1, align 4
  ret i64 %num3

merge:                                            ; preds = %entry
  %num4 = load i64, ptr %num1, align 4
  %num5 = load i64, ptr %num1, align 4
  %subtmp = sub i64 %num5, 1
  %calltmp = call i64 @Math_factorial(ptr %0, i64 %subtmp)
  %multmp = mul i64 %num4, %calltmp
  ret i64 %multmp
}

define void @HelperLib_SayHello(ptr %0) {
entry:
  %print_tmp = call i32 (ptr, ...) @printf(ptr @fmt_str.29, ptr @str_lit.28)
  ret void
}

define i32 @main() {
entry:
  %0 = call i64 @Program_Main()
  ret i32 0
}
