import { Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-stat-row',
    imports: [ReactiveFormsModule],
    templateUrl: './stat-row.component.html',
    styleUrl: './stat-row.component.css'
})
export class StatRowComponent {
  statForm = new FormGroup({
    minValue: new FormControl(0),
    maxValue: new FormControl(),
    weight: new FormControl(),
  })

  test() {
    alert(this.statForm.value.minValue);
  }
}
