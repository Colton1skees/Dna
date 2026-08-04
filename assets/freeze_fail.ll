; ModuleID = 'outmodule5b8d274d-4ff2-4a35-a510-c91679c476f9'
source_filename = "outmodule5b8d274d-4ff2-4a35-a510-c91679c476f9"
target datalayout = "e-m:e-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu-elf"

@memory = common local_unnamed_addr global ptr null

declare void @vmp_branch(i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64) local_unnamed_addr

; Function Attrs: nocallback nocreateundeforpoison nofree nosync nounwind speculatable willreturn memory(none)
declare i8 @llvm.ctpop.i8(i8) #0

declare void @vmp_vmexit(i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64, i64) local_unnamed_addr

; Function Attrs: nocallback nocreateundeforpoison nofree nosync nounwind speculatable willreturn memory(none)
declare i32 @llvm.fshl.i32(i32, i32, i32) #0

define void @PartialCfg(i64 %0, i64 %1, i64 %2, i64 %3, i64 %4, i64 %5, i64 %6, i64 %7, i64 %8, i64 %9, i64 %10, i64 %11, i64 %12, i64 %13, i64 %14, i64 %15, i64 %16, ptr noalias writeonly captures(none) %17, ptr noalias writeonly captures(none) %18, ptr noalias writeonly captures(none) %19, ptr noalias writeonly captures(none) %20, ptr noalias writeonly captures(none) %21, ptr noalias writeonly captures(none) %22, ptr noalias writeonly captures(none) %23, ptr noalias writeonly captures(none) %24, ptr noalias writeonly captures(none) %25, ptr noalias writeonly captures(none) %26, ptr noalias writeonly captures(none) %27, ptr noalias writeonly captures(none) %28, ptr noalias writeonly captures(none) %29, ptr noalias writeonly captures(none) %30, ptr noalias writeonly captures(none) %31, ptr noalias writeonly captures(none) %32, ptr noalias writeonly captures(none) %33) local_unnamed_addr {
entry:
  %mem = load ptr, ptr @memory, align 8
  %34 = getelementptr i8, ptr %mem, i64 %0
  %35 = getelementptr i8, ptr %34, i64 -8
  %36 = getelementptr i8, ptr %34, i64 -16
  %37 = getelementptr i8, ptr %34, i64 -24
  store i64 %10, ptr %37, align 8
  %38 = getelementptr i8, ptr %34, i64 -32
  store i64 %8, ptr %38, align 8
  %39 = getelementptr i8, ptr %34, i64 -40
  store i64 %13, ptr %39, align 8
  %40 = getelementptr i8, ptr %34, i64 -48
  store i64 %6, ptr %40, align 8
  %41 = getelementptr i8, ptr %34, i64 -56
  store i64 %9, ptr %41, align 8
  %42 = getelementptr i8, ptr %34, i64 -64
  store i64 %1, ptr %42, align 8
  %43 = getelementptr i8, ptr %34, i64 -72
  store i64 %4, ptr %43, align 8
  %44 = getelementptr i8, ptr %34, i64 -80
  store i64 %11, ptr %44, align 8
  %45 = getelementptr i8, ptr %34, i64 -88
  store i64 %15, ptr %45, align 8
  %46 = getelementptr i8, ptr %34, i64 -96
  store i64 %16, ptr %46, align 8
  %47 = getelementptr i8, ptr %34, i64 -104
  store i64 %3, ptr %47, align 8
  %48 = getelementptr i8, ptr %34, i64 -112
  store i64 %12, ptr %48, align 8
  %49 = getelementptr i8, ptr %34, i64 -120
  store i64 %7, ptr %49, align 8
  %50 = getelementptr i8, ptr %34, i64 -128
  store i64 0, ptr %50, align 8
  %51 = getelementptr i8, ptr %34, i64 -136
  store i64 %2, ptr %51, align 8
  %52 = add i64 %0, -152
  %53 = getelementptr inbounds i8, ptr %mem, i64 %52
  store i64 0, ptr %53, align 8
  %54 = add i64 %0, -12583064
  %55 = getelementptr i8, ptr %mem, i64 %54
  %56 = getelementptr i8, ptr %55, i64 -8
  %57 = getelementptr i8, ptr %55, i64 112
  %58 = getelementptr i8, ptr %55, i64 168
  %59 = getelementptr i8, ptr %55, i64 16
  %60 = getelementptr i8, ptr %55, i64 176
  %61 = getelementptr i8, ptr %55, i64 160
  %62 = getelementptr i8, ptr %55, i64 48
  %63 = getelementptr i8, ptr %55, i64 64
  %64 = getelementptr i8, ptr %55, i64 136
  %65 = getelementptr i8, ptr %55, i64 128
  %66 = getelementptr i8, ptr %55, i64 88
  %67 = getelementptr i8, ptr %55, i64 24
  %68 = getelementptr i8, ptr %55, i64 40
  %69 = getelementptr i8, ptr %55, i64 56
  %70 = getelementptr i8, ptr %55, i64 144
  %71 = getelementptr i8, ptr %55, i64 72
  %72 = add i64 %0, -16
  %73 = getelementptr i8, ptr %55, i64 152
  %74 = getelementptr i8, ptr %55, i64 32
  %75 = getelementptr i8, ptr %55, i64 96
  %76 = getelementptr inbounds i8, ptr %mem, i64 %72
  %77 = getelementptr i8, ptr %76, i64 8
  store i64 5368723818, ptr %77, align 8
  %78 = getelementptr i8, ptr %55, i64 120
  %79 = getelementptr i8, ptr %55, i64 192
  %80 = getelementptr i8, ptr %55, i64 200
  store i64 %0, ptr %80, align 8
  %81 = getelementptr i8, ptr %55, i64 208
  store i64 %9, ptr %81, align 8
  %82 = getelementptr i8, ptr %55, i64 216
  store i64 %15, ptr %82, align 8
  %83 = trunc i64 %12 to i32
  %84 = getelementptr i8, ptr %55, i64 224
  store i64 %12, ptr %84, align 8
  %85 = getelementptr i8, ptr %55, i64 232
  store i64 %10, ptr %85, align 8
  %86 = trunc i64 %11 to i32
  %87 = getelementptr i8, ptr %34, i64 -12
  %88 = xor i32 %86, -1
  %89 = getelementptr i8, ptr %34, i64 -4
  %90 = add i32 %83, %88
  %91 = trunc i32 %90 to i8
  store i32 %90, ptr %89, align 4
  %92 = xor i8 %91, -1
  %93 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %92)
  %cmp.i.i.i149.i.i.i.i.not = icmp eq i32 %86, %83
  %94 = shl nuw nsw i8 %93, 2
  %95 = and i8 %94, 4
  %96 = xor i8 %95, 4
  %97 = zext nneg i8 %96 to i64
  %98 = icmp slt i32 %90, 0
  %99 = or disjoint i64 %97, 64
  %100 = select i1 %cmp.i.i.i149.i.i.i.i.not, i64 %99, i64 %97
  %101 = or disjoint i64 %97, 128
  %102 = select i1 %98, i64 %100, i64 %101
  store i64 %102, ptr %87, align 8
  store i64 %102, ptr %74, align 8
  %103 = getelementptr i8, ptr %55, i64 184
  %104 = getelementptr i8, ptr %55, i64 80
  %105 = getelementptr i8, ptr %55, i64 104
  %106 = getelementptr i8, ptr %55, i64 8
  %107 = getelementptr i8, ptr %34, i64 -18
  %108 = select i1 %cmp.i.i.i149.i.i.i.i.not, i64 0, i64 68
  store i64 %108, ptr %107, align 8
  %109 = getelementptr i8, ptr %34, i64 -160
  %110 = xor i32 %83, -1
  br i1 %cmp.i.i.i149.i.i.i.i.not, label %.thread, label %"14001337A_assumption"

common.ret:                                       ; preds = %reprove_new_edge_for_jmp_table_14000D20F, %reprove_new_edge_for_jmp_table_14000D02C, %reprove_new_edge_for_jmp_table_14000D112, %reprove_new_edge_for_jmp_table_14000E49C, %reprove_new_edge_for_jmp_table_14000E2D0, %120
  ret void

bb_140014899:                                     ; preds = %LeafBlock38362
  store i64 %.fr38728, ptr %61, align 8
  store i64 %220, ptr %55, align 8
  store i64 %4, ptr %60, align 8
  store i64 %16, ptr %75, align 8
  store i64 %3, ptr %68, align 8
  store i64 %9, ptr %64, align 8
  store i64 %15, ptr %69, align 8
  store i64 %8, ptr %71, align 8
  store i64 %5, ptr %65, align 8
  store i64 %2, ptr %67, align 8
  store i64 %6, ptr %73, align 8
  store i64 %12, ptr %58, align 8
  store i64 %284, ptr %104, align 8
  store i64 %1, ptr %66, align 8
  store i64 %266, ptr %106, align 8
  store i64 %7, ptr %63, align 8
  store i64 %283, ptr %70, align 8
  store i64 %8, ptr %105, align 8
  %111 = trunc i64 %233 to i8
  %112 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %111)
  %113 = xor i64 %233, %.fr38728
  %114 = and i64 %113, 16
  %115 = shl nuw nsw i8 %112, 2
  %116 = and i8 %115, 4
  %117 = xor i8 %116, 4
  %118 = zext nneg i8 %117 to i64
  %119 = or disjoint i64 %114, %118
  br label %120

120:                                              ; preds = %bb_140014899, %.thread
  %phiofops3870238706 = phi i64 [ 5368723818, %.thread ], [ %233, %bb_140014899 ]
  %121 = phi i64 [ %11, %.thread ], [ %282, %bb_140014899 ]
  %122 = phi i64 [ 68, %.thread ], [ %266, %bb_140014899 ]
  %123 = phi i64 [ %10, %.thread ], [ %284, %bb_140014899 ]
  %124 = phi i64 [ %13, %.thread ], [ %220, %bb_140014899 ]
  %125 = phi i64 [ 0, %.thread ], [ %.fr38728, %bb_140014899 ]
  %126 = phi i64 [ 4, %.thread ], [ %119, %bb_140014899 ]
  store i64 %126, ptr %78, align 8
  store i64 %phiofops3870238706, ptr %79, align 8
  %add.i.i164.i.i.i.i = add i64 %125, 5368724303
  %127 = trunc i64 %add.i.i164.i.i.i.i to i8
  %128 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %127)
  %129 = xor i64 %add.i.i164.i.i.i.i, %125
  %130 = and i64 %129, 16
  store i64 %add.i.i164.i.i.i.i, ptr %35, align 8
  %131 = shl nuw nsw i8 %128, 2
  %132 = and i8 %131, 4
  %133 = xor i8 %132, 4
  %134 = zext nneg i8 %133 to i64
  %135 = or disjoint i64 %130, %134
  store i64 %135, ptr %57, align 8
  store i64 %8, ptr %36, align 8
  store i64 %3, ptr %37, align 8
  store i64 %123, ptr %38, align 8
  store i64 %2, ptr %39, align 8
  store i64 %9, ptr %40, align 8
  store i64 %16, ptr %41, align 8
  store i64 %122, ptr %42, align 8
  store i64 %121, ptr %43, align 8
  store i64 %124, ptr %44, align 8
  store i64 %4, ptr %45, align 8
  store i64 %1, ptr %46, align 8
  store i64 %12, ptr %47, align 8
  store i64 %6, ptr %48, align 8
  store i64 %15, ptr %49, align 8
  store i64 %5, ptr %50, align 8
  store i64 %7, ptr %51, align 8
  store i64 5369309510, ptr %56, align 8
  store i64 %0, ptr %17, align 8
  store i64 %1, ptr %18, align 8
  store i64 %2, ptr %19, align 8
  store i64 %3, ptr %20, align 8
  store i64 %4, ptr %21, align 8
  store i64 %5, ptr %22, align 8
  store i64 %6, ptr %23, align 8
  store i64 %7, ptr %24, align 8
  store i64 %8, ptr %25, align 8
  store i64 %9, ptr %26, align 8
  store i64 %123, ptr %27, align 8
  store i64 %121, ptr %28, align 8
  store i64 %12, ptr %29, align 8
  store i64 %124, ptr %30, align 8
  store i64 %add.i.i164.i.i.i.i, ptr %31, align 8
  store i64 %15, ptr %32, align 8
  store i64 %16, ptr %33, align 8
  tail call void @vmp_vmexit(i64 %0, i64 %1, i64 %2, i64 %3, i64 %4, i64 %5, i64 %6, i64 %7, i64 %8, i64 %9, i64 %123, i64 %121, i64 %12, i64 %124, i64 %add.i.i164.i.i.i.i, i64 %15, i64 %16, i64 5368793009)
  br label %common.ret

bb_14000D5BE:                                     ; preds = %"14000E3B2_assumption", %"14001337A_assumption"
  %136 = phi i64 [ %11, %"14001337A_assumption" ], [ %282, %"14000E3B2_assumption" ]
  %137 = phi i32 [ %360, %"14001337A_assumption" ], [ %234, %"14000E3B2_assumption" ]
  %.138342 = phi i64 [ 5368820764, %"14001337A_assumption" ], [ %402, %"14000E3B2_assumption" ]
  %local_state_struct.sroa.253.2328.extract.trunc289.i.i.i = trunc i64 %.138342 to i8
  %xor3.i.i165.i.i.i.i = xor i8 %local_state_struct.sroa.253.2328.extract.trunc289.i.i.i, -86
  %sub.i.i381.i.i.i.i = add i8 %xor3.i.i165.i.i.i.i, 28
  %xor3.i.i147.i.i.i.i = xor i8 %sub.i.i381.i.i.i.i, 6
  %sub.i.i.i.i.i.i6174 = add i8 %xor3.i.i147.i.i.i.i, -1
  %or3.i96.i.i.i.i.i6175 = tail call i8 @llvm.fshl.i8(i8 %sub.i.i.i.i.i.i6174, i8 %sub.i.i.i.i.i.i6174, i8 1)
  %138 = xor i8 %or3.i96.i.i.i.i.i6175, -1
  %139 = zext i8 %138 to i64
  %140 = getelementptr i8, ptr %55, i64 %139
  store i64 0, ptr %140, align 8
  store i64 %4, ptr %73, align 8
  store i64 %16, ptr %57, align 8
  store i64 %3, ptr %62, align 8
  store i64 %9, ptr %70, align 8
  store i64 %15, ptr %55, align 8
  store i64 %8, ptr %60, align 8
  store i64 %5, ptr %105, align 8
  store i64 %2, ptr %103, align 8
  store i64 %6, ptr %106, align 8
  store i64 %12, ptr %75, align 8
  store i64 %1, ptr %59, align 8
  store i64 %7, ptr %64, align 8
  store i64 %136, ptr %71, align 8
  %141 = load i64, ptr %66, align 8
  %142 = add i64 %141, 5368723818
  store i64 %142, ptr %79, align 8
  %143 = trunc i64 %136 to i32
  %144 = and i32 %143, 234324
  %145 = mul i32 %144, %316
  store i64 %346, ptr %63, align 8
  %146 = add i32 %145, %137
  %147 = xor i64 %136, 234523
  %148 = xor i32 %143, 234523
  %149 = xor i32 %143, -234524
  %150 = trunc i64 %136 to i8
  %151 = xor i8 %150, -28
  %152 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %151)
  %153 = icmp eq i32 %143, -234524
  %154 = shl nuw nsw i8 %152, 2
  %155 = and i8 %154, 4
  %156 = xor i8 %155, 4
  %157 = zext nneg i8 %156 to i64
  %158 = icmp slt i32 %143, 0
  %159 = or disjoint i64 %157, 64
  %160 = select i1 %153, i64 %159, i64 %157
  %161 = or disjoint i64 %157, 128
  %162 = select i1 %158, i64 %160, i64 %161
  store i64 %162, ptr %61, align 8
  %163 = or i32 %149, %348
  %164 = xor i32 %163, -1
  %165 = and i64 %147, %15
  %166 = and i32 %148, %316
  %167 = trunc i32 %164 to i8
  %168 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %167)
  %169 = icmp eq i32 %163, -1
  %170 = shl nuw nsw i8 %168, 2
  %171 = and i8 %170, 4
  %172 = xor i8 %171, 4
  %173 = zext nneg i8 %172 to i32
  %174 = lshr i32 %164, 24
  %175 = and i32 %174, 128
  %176 = select i1 %169, i32 64, i32 %175
  %177 = or disjoint i32 %176, %173
  %178 = zext nneg i32 %177 to i64
  store i64 %178, ptr %78, align 8
  store i32 %146, ptr %74, align 4
  %179 = shl i64 %165, 32
  %180 = ashr exact i64 %179, 32
  %181 = mul nsw i64 %180, %351
  %182 = mul i64 %165, %8
  %183 = mul i32 %166, %349
  %184 = lshr i64 %181, 32
  %185 = trunc nuw i64 %184 to i32
  store i32 %185, ptr %78, align 4
  store i32 %183, ptr %68, align 4
  store i32 0, ptr %347, align 4
  store i32 0, ptr %315, align 4
  store i64 %352, ptr %67, align 8
  store i64 132, ptr %65, align 8
  store i64 %354, ptr %58, align 8
  %186 = add i64 %141, 5368762895
  %187 = add i64 %141, 5368762642
  %188 = add i64 %141, %.v
  %189 = select i1 %318, i64 %187, i64 %186
  %190 = icmp ult i64 %189, %141
  %191 = trunc i64 %186 to i8
  %192 = trunc i64 %187 to i8
  %193 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %191)
  %194 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %192)
  %195 = select i1 %318, i8 %194, i8 %193
  %196 = xor i64 %189, %141
  %197 = and i64 %196, 16
  %198 = xor i64 %197, 16
  %199 = select i1 %318, i64 %198, i64 %197
  %200 = icmp eq i64 %189, 0
  %201 = lshr i64 %196, 63
  %202 = lshr i64 %189, 63
  %203 = add nuw nsw i64 %201, %202
  %204 = icmp eq i64 %203, 2
  %205 = shl nuw nsw i8 %195, 2
  %206 = and i8 %205, 4
  %207 = xor i8 %206, 4
  %208 = zext nneg i8 %207 to i64
  %209 = lshr i64 %189, 56
  %210 = and i64 %209, 128
  %211 = zext i1 %190 to i64
  %212 = or disjoint i64 %210, %211
  %213 = or disjoint i64 %212, 64
  %214 = select i1 %200, i64 %213, i64 %212
  %215 = or disjoint i64 %214, %199
  %216 = or disjoint i64 %215, 2048
  %217 = select i1 %204, i64 %216, i64 %215
  %218 = or disjoint i64 %217, %208
  store i64 %218, ptr %69, align 8
  store i64 %188, ptr %104, align 8
  store i64 %218, ptr %35, align 8
  store i64 %352, ptr %76, align 8
  store i64 %8, ptr %314, align 8
  store i64 %12, ptr %313, align 8
  store i64 %6, ptr %312, align 8
  store i64 %2, ptr %311, align 8
  store i64 %346, ptr %310, align 8
  store i64 %16, ptr %309, align 8
  store i64 %9, ptr %308, align 8
  store i64 %4, ptr %307, align 8
  %219 = and i64 %182, 4294967295
  store i64 %219, ptr %306, align 8
  store i64 %7, ptr %305, align 8
  %220 = zext i32 %146 to i64
  store i64 %220, ptr %304, align 8
  store i64 %136, ptr %303, align 8
  store i64 %5, ptr %302, align 8
  store i64 %15, ptr %301, align 8
  store i64 %1, ptr %300, align 8
  store i64 %3, ptr %299, align 8
  store i64 %141, ptr %53, align 8
  store i64 %188, ptr %355, align 8
  store i64 5369901065, ptr %56, align 8
  %221 = add i64 %188, -4
  %222 = getelementptr inbounds i8, ptr %mem, i64 %221
  %223 = load i32, ptr %222, align 4
  %224 = trunc i64 %188 to i32
  %225 = trunc i64 %141 to i32
  %226 = add i32 %362, %225
  %227 = xor i32 %223, %224
  %xor3.i.i20.i.i.i.i = xor i32 %227, 618080013
  %or3.i98.i.i.i.i.i9993 = tail call i32 @llvm.fshl.i32(i32 %xor3.i.i20.i.i.i.i, i32 %xor3.i.i20.i.i.i.i, i32 3)
  %sub.i.i.i.i.i.i9994 = sub i32 0, %or3.i98.i.i.i.i.i9993
  %or3.i100.i.i.i.i.i9995 = tail call i32 @llvm.fshl.i32(i32 %sub.i.i.i.i.i.i9994, i32 %sub.i.i.i.i.i.i9994, i32 30)
  store i64 %188, ptr %56, align 8
  %228 = xor i32 %or3.i100.i.i.i.i.i9995, %226
  store i32 %228, ptr %56, align 4
  %Pivot = icmp slt i64 %221, 5368762891
  br i1 %Pivot, label %LeafBlock38352, label %LeafBlock38354

LeafBlock38354:                                   ; preds = %bb_14000D5BE
  %SwitchLeaf38355 = icmp eq i64 %221, 5368762891
  br i1 %SwitchLeaf38355, label %"14000D20B_assumption", label %reprove_new_edge_for_jmp_table_14000D20F

LeafBlock38352:                                   ; preds = %bb_14000D5BE
  %SwitchLeaf38353 = icmp eq i64 %221, 5368762638
  br i1 %SwitchLeaf38353, label %"14000D10E_assumption", label %reprove_new_edge_for_jmp_table_14000D20F

bb_14000E787:                                     ; preds = %"14000D10E_assumption.bb_14000E787_crit_edge", %"14000D20B_assumption"
  %.pre-phi.pre-phi.pre-phi.pre-phi.pre-phi = phi i32 [ %.pre, %"14000D10E_assumption.bb_14000E787_crit_edge" ], [ %356, %"14000D20B_assumption" ]
  %.038350 = phi i64 [ 5369120249, %"14000D10E_assumption.bb_14000E787_crit_edge" ], [ %384, %"14000D20B_assumption" ]
  %local_state_struct.sroa.376.2344.extract.trunc402.i.i.i = trunc i64 %.038350 to i8
  %not.i.i.i.i.i.i11840 = xor i8 %local_state_struct.sroa.376.2344.extract.trunc402.i.i.i, 84
  %sub.i.i335.i.i.i.i11841 = add i8 %not.i.i.i.i.i.i11840, -1
  %or3.i96.i274.i.i.i.i = tail call i8 @llvm.fshl.i8(i8 %sub.i.i335.i.i.i.i11841, i8 %sub.i.i335.i.i.i.i11841, i8 1)
  %229 = xor i8 %or3.i96.i274.i.i.i.i, -23
  %230 = zext i8 %229 to i64
  %231 = getelementptr i8, ptr %55, i64 %230
  store i64 %141, ptr %231, align 8
  store i64 %3, ptr %74, align 8
  store i64 %1, ptr %106, align 8
  store i64 %15, ptr %103, align 8
  store i64 %5, ptr %66, align 8
  store i64 %220, ptr %65, align 8
  store i64 %7, ptr %55, align 8
  store i64 %4, ptr %64, align 8
  store i64 %9, ptr %67, align 8
  store i64 %16, ptr %78, align 8
  store i64 %2, ptr %104, align 8
  store i64 %6, ptr %105, align 8
  store i64 %12, ptr %63, align 8
  store i64 %8, ptr %73, align 8
  %232 = load i64, ptr %60, align 8
  %.fr38728 = freeze i64 %232
  %233 = add i64 %.fr38728, 5368723818
  store i64 %233, ptr %79, align 8
  %234 = add i32 %146, %.pre-phi.pre-phi.pre-phi.pre-phi.pre-phi
  %235 = add i32 %143, 1
  %236 = add i64 %136, 1
  store i32 %235, ptr %70, align 4
  store i32 0, ptr %358, align 4
  %237 = sub i32 %143, %83
  %238 = icmp ult i32 %237, %110
  %239 = trunc i32 %237 to i8
  %240 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %239)
  %241 = xor i32 %237, %110
  %242 = xor i32 %241, %235
  %243 = and i32 %242, 16
  %244 = lshr i32 %241, 31
  %245 = xor i32 %237, %235
  %246 = lshr i32 %245, 31
  %247 = add nuw nsw i32 %244, %246
  %248 = icmp eq i32 %247, 2
  %249 = shl nuw nsw i8 %240, 2
  %250 = and i8 %249, 4
  %251 = xor i8 %250, 4
  %252 = zext nneg i8 %251 to i32
  %253 = select i1 %238, i32 1, i32 0
  %254 = zext i1 %238 to i32
  %255 = or disjoint i32 %243, %253
  %256 = or disjoint i32 %255, 2048
  %257 = select i1 %248, i32 %256, i32 %255
  %258 = xor i32 %237, -1
  %259 = icmp eq i32 %237, -1
  %260 = lshr i32 %258, 24
  %261 = and i32 %260, 128
  %262 = icmp slt i32 %237, -1
  %263 = select i1 %262, i64 68, i64 0
  store i64 %263, ptr %62, align 8
  %.v39164 = select i1 %259, i32 64, i32 %261
  %264 = or disjoint i32 %.v39164, %252
  %265 = or disjoint i32 %264, %257
  %266 = zext nneg i32 %265 to i64
  %267 = trunc i32 %265 to i8
  %268 = tail call range(i8 0, 6) i8 @llvm.ctpop.i8(i8 %267)
  %269 = icmp eq i32 %265, 0
  store i64 %266, ptr %87, align 8
  %270 = shl nuw nsw i8 %268, 2
  %271 = and i8 %270, 4
  %272 = xor i8 %271, 4
  %273 = zext nneg i8 %272 to i64
  %274 = or disjoint i64 %273, 64
  %275 = select i1 %269, i64 %274, i64 %273
  store i64 %275, ptr %338, align 8
  store i64 %275, ptr %57, align 8
  store i64 %266, ptr %75, align 8
  store i32 %234, ptr %68, align 4
  store i32 0, ptr %347, align 4
  %276 = select i1 %259, i64 0, i64 64
  store i64 %276, ptr %359, align 8
  %277 = select i1 %259, i64 -1, i64 0
  %278 = sext i1 %259 to i64
  store i64 %277, ptr %58, align 8
  %279 = select i1 %259, i64 68, i64 0
  store i64 %279, ptr %69, align 8
  %280 = select i1 %259, i64 0, i64 68
  store i64 %280, ptr %71, align 8
  store i64 0, ptr %59, align 8
  %.v38988 = select i1 %259, i64 5368793245, i64 5368767414
  %281 = add i64 %.fr38728, %.v38988
  store i64 %281, ptr %61, align 8
  store i64 %8, ptr %35, align 8
  store i64 %5, ptr %76, align 8
  %282 = zext i32 %235 to i64
  %283 = and i64 %236, 4294967295
  store i64 %283, ptr %314, align 8
  store i64 %7, ptr %313, align 8
  store i64 %266, ptr %312, align 8
  store i64 %1, ptr %311, align 8
  %284 = zext i32 %234 to i64
  store i64 %284, ptr %310, align 8
  store i64 %12, ptr %309, align 8
  store i64 %6, ptr %308, align 8
  store i64 %2, ptr %307, align 8
  store i64 %5, ptr %306, align 8
  store i64 %8, ptr %305, align 8
  store i64 %15, ptr %304, align 8
  store i64 %9, ptr %303, align 8
  store i64 %3, ptr %302, align 8
  store i64 %16, ptr %301, align 8
  store i64 %4, ptr %300, align 8
  store i64 %220, ptr %299, align 8
  store i64 %.fr38728, ptr %53, align 8
  store i64 %281, ptr %109, align 8
  store i64 5368917231, ptr %56, align 8
  %285 = add i64 %281, -4
  %286 = getelementptr inbounds i8, ptr %mem, i64 %285
  %287 = load i32, ptr %286, align 4
  %288 = trunc i64 %.fr38728 to i32
  %289 = select i1 %259, i32 1073825949, i32 1073800118
  %290 = add i32 %289, %288
  %291 = xor i32 %287, %290
  %292 = add i32 %291, 1294535963
  store i64 %281, ptr %56, align 8
  %293 = xor i32 %292, %290
  store i32 %293, ptr %56, align 4
  %Pivot38365 = icmp slt i64 %285, 5368793241
  br i1 %Pivot38365, label %LeafBlock38360, label %LeafBlock38362

LeafBlock38362:                                   ; preds = %bb_14000E787
  %294 = trunc i64 %281 to i32
  %xor3.i.i44.i.i.i.i.le38804 = xor i32 %287, %294
  %add.i.i246.i.i.i.i14984.le = add i32 %xor3.i.i44.i.i.i.i.le38804, 1294535963
  %xor3.i.i.i.i.i.i14985.le38797 = xor i32 %add.i.i246.i.i.i.i14984.le, %294
  %295 = zext i32 %xor3.i.i.i.i.i.i14985.le38797 to i64
  %296 = and i64 %281, -4294967296
  %297 = or disjoint i64 %296, %295
  %SwitchLeaf38363 = icmp eq i64 %285, 5368793241
  br i1 %SwitchLeaf38363, label %bb_140014899, label %reprove_new_edge_for_jmp_table_14000E49C

LeafBlock38360:                                   ; preds = %bb_14000E787
  %SwitchLeaf38361 = icmp eq i64 %285, 5368767410
  br i1 %SwitchLeaf38361, label %"14000E3B2_assumption", label %reprove_new_edge_for_jmp_table_14000E49C.loopexit

.thread:                                          ; preds = %entry
  %298 = getelementptr i8, ptr %34, i64 -144
  store i64 %4, ptr %62, align 8
  store i64 %9, ptr %78, align 8
  store i64 %8, ptr %103, align 8
  store i64 %7, ptr %57, align 8
  store i64 %8, ptr %59, align 8
  store i64 68, ptr %35, align 8
  store i64 0, ptr %76, align 8
  store i64 %11, ptr %37, align 8
  store i64 %7, ptr %38, align 8
  store i64 %13, ptr %298, align 8
  store i64 5368793245, ptr %109, align 8
  store i64 0, ptr %61, align 8
  store i64 %13, ptr %55, align 8
  store i64 %4, ptr %60, align 8
  store i64 %16, ptr %75, align 8
  store i64 %3, ptr %68, align 8
  store i64 %9, ptr %64, align 8
  store i64 %15, ptr %69, align 8
  store i64 %8, ptr %71, align 8
  store i64 %5, ptr %65, align 8
  store i64 %2, ptr %67, align 8
  store i64 %6, ptr %73, align 8
  store i64 %12, ptr %58, align 8
  store i64 %10, ptr %104, align 8
  store i64 %1, ptr %66, align 8
  store i64 68, ptr %106, align 8
  store i64 %7, ptr %63, align 8
  store i64 %11, ptr %70, align 8
  store i64 68, ptr %105, align 8
  br label %120

"14001337A_assumption":                           ; preds = %entry
  store i64 4, ptr %66, align 8
  %299 = getelementptr i8, ptr %53, i64 8
  %300 = getelementptr i8, ptr %53, i64 16
  %301 = getelementptr i8, ptr %53, i64 24
  %302 = getelementptr i8, ptr %53, i64 32
  %303 = getelementptr i8, ptr %53, i64 40
  %304 = getelementptr i8, ptr %53, i64 48
  %305 = getelementptr i8, ptr %53, i64 56
  %306 = getelementptr i8, ptr %53, i64 64
  %307 = getelementptr i8, ptr %53, i64 72
  %308 = getelementptr i8, ptr %53, i64 80
  %309 = getelementptr i8, ptr %53, i64 88
  %310 = getelementptr i8, ptr %53, i64 96
  %311 = getelementptr i8, ptr %53, i64 104
  %312 = getelementptr i8, ptr %53, i64 112
  %313 = getelementptr i8, ptr %53, i64 120
  %314 = getelementptr i8, ptr %53, i64 128
  %315 = getelementptr i8, ptr %55, i64 36
  %316 = trunc i64 %15 to i32
  %317 = sub i32 6, %83
  %318 = icmp ugt i32 %83, 6
  %319 = icmp ult i32 %83, 7
  %320 = trunc i32 %317 to i8
  %321 = tail call range(i8 0, 9) i8 @llvm.ctpop.i8(i8 %320)
  %322 = xor i32 %317, %83
  %323 = xor i32 %322, -1
  %324 = and i32 %323, 16
  %325 = lshr i32 %323, 31
  %326 = lshr i32 %317, 31
  %327 = add nuw nsw i32 %325, %326
  %328 = icmp eq i32 %327, 2
  %329 = shl nuw nsw i8 %321, 2
  %330 = and i8 %329, 4
  %331 = xor i8 %330, 4
  %332 = zext nneg i8 %331 to i32
  %333 = select i1 %318, i32 0, i32 1
  %334 = zext i1 %319 to i32
  %335 = or disjoint i32 %324, %333
  %336 = or disjoint i32 %335, 2048
  %337 = select i1 %328, i32 %336, i32 %335
  %338 = getelementptr i8, ptr %34, i64 -20
  %339 = icmp eq i32 %83, 7
  %340 = icmp slt i32 %317, 0
  %341 = or disjoint i32 %337, %332
  %342 = zext nneg i32 %341 to i64
  %343 = or disjoint i64 %342, 64
  %344 = select i1 %339, i64 %343, i64 %342
  %345 = or disjoint i64 %342, 128
  %346 = select i1 %340, i64 %344, i64 %345
  %347 = getelementptr i8, ptr %55, i64 44
  %348 = xor i32 %316, -1
  %349 = trunc i64 %8 to i32
  %350 = shl i64 %8, 32
  %351 = ashr exact i64 %350, 32
  %352 = select i1 %318, i64 -1, i64 0
  %353 = sext i1 %318 to i64
  %354 = select i1 %318, i64 4, i64 68
  %355 = getelementptr i8, ptr %53, i64 -8
  %356 = trunc i64 %6 to i32
  %357 = getelementptr i8, ptr %55, i64 60
  %358 = getelementptr i8, ptr %55, i64 148
  %359 = getelementptr i8, ptr %53, i64 142
  %360 = trunc i64 %10 to i32
  %361 = and i64 %6, 4294967295
  %.v = select i1 %318, i64 5368762642, i64 5368762895
  %362 = select i1 %318, i32 1073795346, i32 1073795599
  br label %bb_14000D5BE

reprove_new_edge_for_jmp_table_14000D20F:         ; preds = %LeafBlock38354, %LeafBlock38352
  %xor3.i.i.i.i.i.i9996.le = xor i32 %or3.i100.i.i.i.i.i9995, %224
  %363 = add i64 %0, -12582744
  %364 = zext i32 %xor3.i.i.i.i.i.i9996.le to i64
  %365 = and i64 %188, -4294967296
  %366 = or disjoint i64 %365, %364
  %conv.i12.i.i.i.i.i9997.le = sext i32 %or3.i100.i.i.i.i.i9995 to i64
  %add.i.i135.i.i.i.i.le = add nsw i64 %conv.i12.i.i.i.i.i9997.le, 5369339866
  tail call void @vmp_branch(i64 %54, i64 0, i64 %2, i64 %3, i64 %4, i64 %5, i64 %221, i64 %363, i64 %conv.i12.i.i.i.i.i9997.le, i64 %366, i64 %52, i64 %188, i64 %188, i64 80, i64 %add.i.i135.i.i.i.i.le, i64 %72, i64 %add.i.i135.i.i.i.i.le, i64 5368762895)
  br label %common.ret

"14000D10E_assumption":                           ; preds = %LeafBlock38352
  store i64 %141, ptr %73, align 8
  store i64 %3, ptr %75, align 8
  store i64 %1, ptr %67, align 8
  store i64 %15, ptr %60, align 8
  store i64 %5, ptr %63, align 8
  store i64 %136, ptr %103, align 8
  store i64 %220, ptr %78, align 8
  store i64 %7, ptr %66, align 8
  store i64 %219, ptr %58, align 8
  store i64 %4, ptr %61, align 8
  store i64 %9, ptr %105, align 8
  store i64 %16, ptr %104, align 8
  store i64 %346, ptr %55, align 8
  store i64 %2, ptr %74, align 8
  store i64 %6, ptr %62, align 8
  store i64 %12, ptr %65, align 8
  store i64 %8, ptr %70, align 8
  store i64 %352, ptr %59, align 8
  store i64 %218, ptr %71, align 8
  %367 = add i64 %141, 5368768395
  %368 = icmp ult i64 %141, -5368768395
  %369 = select i1 %368, i64 4, i64 5
  store i64 %369, ptr %64, align 8
  store i64 %367, ptr %57, align 8
  store i64 %136, ptr %35, align 8
  store i64 %4, ptr %76, align 8
  store i64 %367, ptr %355, align 8
  store i64 %367, ptr %56, align 8
  %370 = select i1 %368, i32 1074152953, i32 -154747320
  store i32 %370, ptr %56, align 4
  br i1 %368, label %"14000D10E_assumption.bb_14000E787_crit_edge", label %reprove_new_edge_for_jmp_table_14000D02C

"14000D10E_assumption.bb_14000E787_crit_edge":    ; preds = %"14000D10E_assumption"
  %.pre = trunc i64 %182 to i32
  br label %bb_14000E787

"14000D20B_assumption":                           ; preds = %LeafBlock38354
  store i64 %141, ptr %78, align 8
  store i64 %3, ptr %67, align 8
  store i64 %1, ptr %61, align 8
  store i64 %15, ptr %65, align 8
  store i64 %5, ptr %66, align 8
  store i64 %136, ptr %59, align 8
  store i64 %220, ptr %58, align 8
  store i64 %7, ptr %105, align 8
  store i64 %4, ptr %64, align 8
  store i64 %9, ptr %55, align 8
  store i64 %16, ptr %62, align 8
  store i64 %346, ptr %74, align 8
  store i64 %2, ptr %70, align 8
  store i64 %6, ptr %68, align 8
  store i64 %12, ptr %71, align 8
  store i64 %8, ptr %57, align 8
  store i64 %352, ptr %103, align 8
  store i32 %356, ptr %69, align 4
  store i32 0, ptr %357, align 4
  %371 = add i64 %141, 5368768395
  %372 = add i64 %141, 11
  %373 = xor i64 %372, %141
  %374 = and i64 %373, 16
  %375 = or disjoint i64 %374, 4
  store i64 %375, ptr %63, align 8
  store i64 %371, ptr %106, align 8
  store i64 %8, ptr %35, align 8
  store i64 %220, ptr %76, align 8
  store i64 %361, ptr %306, align 8
  store i64 %371, ptr %355, align 8
  store i64 5369921363, ptr %56, align 8
  %376 = add i64 %141, 5368768391
  %377 = getelementptr inbounds i8, ptr %mem, i64 %376
  %378 = load i32, ptr %377, align 4
  %local_state_struct.sroa.278.2344.extract.trunc.i.i.i11966 = trunc i64 %371 to i32
  %379 = add i32 %225, 1073801099
  %xor3.i.i48.i.i.i.i11967 = xor i32 %378, %local_state_struct.sroa.278.2344.extract.trunc.i.i.i11966
  %380 = xor i32 %378, %379
  %add.i.i259.i.i.i.i11968 = add i32 %xor3.i.i48.i.i.i.i11967, 1294535963
  %381 = add i32 %380, 1294535963
  store i64 %371, ptr %56, align 8
  %xor3.i.i.i.i.i.i11969 = xor i32 %add.i.i259.i.i.i.i11968, %local_state_struct.sroa.278.2344.extract.trunc.i.i.i11966
  %382 = xor i32 %381, %379
  store i32 %382, ptr %56, align 4
  %383 = zext i32 %xor3.i.i.i.i.i.i11969 to i64
  %384 = or disjoint i64 %383, 4294967296
  %SwitchLeaf38359 = icmp eq i64 %141, 0
  br i1 %SwitchLeaf38359, label %bb_14000E787, label %reprove_new_edge_for_jmp_table_14000D112

reprove_new_edge_for_jmp_table_14000D02C:         ; preds = %"14000D10E_assumption"
  %385 = add i64 %0, -12582744
  tail call void @vmp_branch(i64 %54, i64 4140411328, i64 %2, i64 %3, i64 %4, i64 %5, i64 8435187272, i64 %385, i64 -1228515130, i64 %52, i64 0, i64 %367, i64 %188, i64 112, i64 4140411328, i64 5368768138, i64 5369921363, i64 5368762412)
  br label %common.ret

reprove_new_edge_for_jmp_table_14000D112:         ; preds = %"14000D20B_assumption"
  %386 = add i64 %0, -12582744
  %conv.i12.i.i.i.i.i11970 = sext i32 %add.i.i259.i.i.i.i11968 to i64
  %add.i.i196.i.i.i.i11971 = add nsw i64 %conv.i12.i.i.i.i.i11970, 5368926458
  tail call void @vmp_branch(i64 %54, i64 %add.i.i196.i.i.i.i11971, i64 %2, i64 %3, i64 %4, i64 %5, i64 %384, i64 %386, i64 %conv.i12.i.i.i.i.i11970, i64 %52, i64 0, i64 %371, i64 143349, i64 8, i64 %add.i.i196.i.i.i.i11971, i64 %376, i64 5369921363, i64 5368762642)
  br label %common.ret

reprove_new_edge_for_jmp_table_14000E49C.loopexit: ; preds = %LeafBlock38360
  %387 = trunc i64 %281 to i32
  %xor3.i.i44.i.i.i.i.le = xor i32 %287, %387
  %add.i.i246.i.i.i.i14984.le38800 = add i32 %xor3.i.i44.i.i.i.i.le, 1294535963
  %xor3.i.i.i.i.i.i14985.le = xor i32 %add.i.i246.i.i.i.i14984.le38800, %387
  %388 = zext i32 %xor3.i.i.i.i.i.i14985.le to i64
  %389 = and i64 %281, -4294967296
  %390 = or disjoint i64 %389, %388
  br label %reprove_new_edge_for_jmp_table_14000E49C

reprove_new_edge_for_jmp_table_14000E49C:         ; preds = %reprove_new_edge_for_jmp_table_14000E49C.loopexit, %LeafBlock38362
  %add.i.i246.i.i.i.i1498438769 = phi i32 [ %add.i.i246.i.i.i.i14984.le38800, %reprove_new_edge_for_jmp_table_14000E49C.loopexit ], [ %add.i.i246.i.i.i.i14984.le, %LeafBlock38362 ]
  %391 = phi i64 [ %390, %reprove_new_edge_for_jmp_table_14000E49C.loopexit ], [ %297, %LeafBlock38362 ]
  %conv.i12.i6.i.i.i.i38645 = sext i32 %add.i.i246.i.i.i.i1498438769 to i64
  %add.i.i189.i.i.i.i38642 = add nsw i64 %conv.i12.i6.i.i.i.i38645, 5368926458
  tail call void @vmp_branch(i64 %54, i64 %add.i.i189.i.i.i.i38642, i64 %2, i64 %3, i64 %4, i64 %5, i64 %391, i64 %281, i64 %conv.i12.i6.i.i.i.i38645, i64 %52, i64 0, i64 %72, i64 %.v38988, i64 %281, i64 %add.i.i189.i.i.i.i38642, i64 %285, i64 160, i64 5368767644)
  br label %common.ret

"14000E3B2_assumption":                           ; preds = %LeafBlock38360
  store i64 %.fr38728, ptr %103, align 8
  store i64 %220, ptr %70, align 8
  store i64 %4, ptr %58, align 8
  store i64 %16, ptr %57, align 8
  store i64 %3, ptr %59, align 8
  store i64 %9, ptr %66, align 8
  store i64 %15, ptr %64, align 8
  store i64 %8, ptr %69, align 8
  store i64 %5, ptr %65, align 8
  store i64 %2, ptr %61, align 8
  store i64 %6, ptr %78, align 8
  store i64 %12, ptr %71, align 8
  store i64 %284, ptr %67, align 8
  store i64 %1, ptr %74, align 8
  store i64 %266, ptr %105, align 8
  store i64 %7, ptr %62, align 8
  store i64 %283, ptr %55, align 8
  store i64 %8, ptr %75, align 8
  %392 = add i64 %.fr38728, 5368763842
  %393 = icmp ugt i64 %.fr38728, -5368763843
  %394 = select i1 %393, i64 5, i64 0
  store i64 %394, ptr %104, align 8
  store i64 %392, ptr %68, align 8
  store i64 %394, ptr %35, align 8
  store i64 %1, ptr %76, align 8
  store i64 %392, ptr %109, align 8
  store i64 5369856980, ptr %56, align 8
  %395 = add i64 %.fr38728, 5368763838
  %396 = getelementptr inbounds i8, ptr %mem, i64 %395
  %397 = load i32, ptr %396, align 4
  %xor3.i.i37.i.i.i.i38964 = xor i32 %397, 1691834326
  %xor3.i.i37.i.i.i.i38965 = xor i32 %397, 1691873999
  %398 = select i1 %393, i32 %xor3.i.i37.i.i.i.i38964, i32 %xor3.i.i37.i.i.i.i38965
  %or3.i98.i.i.i.i.i1590038966 = tail call i32 @llvm.fshl.i32(i32 %398, i32 %xor3.i.i37.i.i.i.i38964, i32 3)
  %or3.i98.i.i.i.i.i1590038967 = tail call i32 @llvm.fshl.i32(i32 %398, i32 %xor3.i.i37.i.i.i.i38965, i32 3)
  %399 = select i1 %393, i32 %or3.i98.i.i.i.i.i1590038966, i32 %or3.i98.i.i.i.i.i1590038967
  %sub.i.i171.i.i.i.i = sub i32 0, %399
  %or3.i100.i.i.i.i.i15901 = tail call i32 @llvm.fshl.i32(i32 %sub.i.i171.i.i.i.i, i32 %sub.i.i171.i.i.i.i, i32 30)
  store i64 %392, ptr %56, align 8
  %.v39184 = select i1 %393, i32 1073770715, i32 1073796546
  %400 = xor i32 %or3.i100.i.i.i.i.i15901, %.v39184
  store i32 %400, ptr %56, align 4
  %401 = zext i32 %400 to i64
  %402 = or disjoint i64 %401, 4294967296
  %SwitchLeaf38367 = icmp eq i64 %.fr38728, 0
  br i1 %SwitchLeaf38367, label %bb_14000D5BE, label %reprove_new_edge_for_jmp_table_14000E2D0

reprove_new_edge_for_jmp_table_14000E2D0:         ; preds = %"14000E3B2_assumption"
  %conv.i12.i8.i.i.i.i15903 = sext i32 %or3.i100.i.i.i.i.i15901 to i64
  %add.i.i109.i.i.i.i = add nsw i64 %conv.i12.i8.i.i.i.i15903, 5369339866
  tail call void @vmp_branch(i64 %54, i64 0, i64 %2, i64 %3, i64 %4, i64 %5, i64 %395, i64 %392, i64 %conv.i12.i8.i.i.i.i15903, i64 %402, i64 %52, i64 %72, i64 5368763842, i64 -485306, i64 %add.i.i109.i.i.i.i, i64 5368767184, i64 %add.i.i109.i.i.i.i, i64 5368767184)
  br label %common.ret
}

; Function Attrs: nocallback nocreateundeforpoison nofree nosync nounwind speculatable willreturn memory(none)
declare i8 @llvm.fshl.i8(i8, i8, i8) #0

attributes #0 = { nocallback nocreateundeforpoison nofree nosync nounwind speculatable willreturn memory(none) }
